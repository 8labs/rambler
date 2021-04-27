namespace Rambler.Test
{
    using System.Collections.Generic;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Server.Utility;
    using System;
    using System.Collections.Concurrent;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;

    [TestClass]
    public class UtilityTest
    {
        public interface ITest<T> { }

        public class TestA : ITest<string> { }

        public class TestB : ITest<bool> { }

        [TestMethod]
        public void ShouldImplementOpenGeneric()
        {
            Assert.IsTrue(typeof(TestA).ImplementsOpenGenericInterface(typeof(ITest<>)));
        }

        [TestMethod]
        public void ShouldFindAllImplementingOpenGeneric()
        {
            var assembly = typeof(TestA).GetTypeInfo().Assembly;
            var results = assembly.GetAllTypesImplementingOpenGenericInterface(typeof(ITest<>));
            Assert.AreEqual(2, results.Count());
        }

        [TestMethod]
        public async Task ShouldQueueParallel()
        {
            var q = new SerialQueue();

            int total = 100;
            int totalX = 0;
            ConcurrentBag<Task<int>> tasks = new ConcurrentBag<Task<int>>();
            Parallel.For(0, total, (x) =>
            {
                tasks.Add(q.Enqueue(() =>
                {
                    totalX += x;
                    return Task.FromResult(x);
                }));
            });

            var test = 0;
            foreach (var t in tasks)
            {
                test += await t;
            }

            Assert.AreEqual(test, totalX);
        }

        [TestMethod]
        public async Task ShouldProcessInOrder()
        {
            var q = new SerialQueue();

            var x = 0;
            var t1 = q.Enqueue(() =>
            {
                x++;
                return Task.FromResult(x);
            });

            var t2 = q.Enqueue(async () =>
            {
                await Task.Delay(1000);
                x++;
                return x;
            });

            var t3 = q.Enqueue(() =>
            {
                x++;
                return Task.FromResult(x);
            });

            Assert.AreEqual(1, await t1);
            Assert.AreEqual(3, await t3); //awaiting out of order, should still fire after 2
            Assert.AreEqual(2, await t2);

        }

        [TestMethod]
        public async Task ShouldContinueOnExceptions()
        {
            var q = new SerialQueue();

            var x = 0;
            var t1 = q.Enqueue(() =>
            {
                x++;
                return Task.FromResult(x);
            });

            var t2 = q.Enqueue<bool>(async () =>
            {
                await Task.Delay(1000);
                x++;
                throw new Exception("2");
            });

            var t3 = q.Enqueue(() =>
            {
                x++;
                return Task.FromResult(x);
            });

            Assert.AreEqual(1, await t1);
            Assert.AreEqual(3, await t3); //awaiting out of order, should still fire after 2
            Assert.ThrowsException<Exception>(() => t2.GetAwaiter().GetResult());
        }

        [TestMethod]
        public async Task ShouldContinueOnIndividualCancels()
        {
            var q = new SerialQueue();

            var x = 0;
            var t1 = q.Enqueue(() =>
            {
                x++;
                return Task.FromResult(x);
            });

            var cancel = new CancellationTokenSource();
            var t2 = q.Enqueue(async () =>
            {
                await Task.Delay(100000, cancel.Token);
                x++;
                return true;
            });

            var t3 = q.Enqueue(() =>
            {
                x++;
                return Task.FromResult(x);
            });

            cancel.Cancel();

            Assert.AreEqual(1, await t1);
            Assert.AreEqual(2, await t3); //awaiting out of order, should still fire after 2
            Assert.ThrowsException<TaskCanceledException>(() => t2.GetAwaiter().GetResult());
        }

        [TestMethod]
        public async Task ShouldCancelMaybe()
        {
            var cancel = new CancellationTokenSource();
            var q = new SerialQueue(cancel.Token);

            var x = 0;
            var t1 = q.Enqueue(() =>
            {
                x++;
                return Task.FromResult(x);
            });
            
            var t2 = q.Enqueue(async () =>
            {
                await Task.Delay(500);
                x++;

                // cancel it within this block to make sure things happen in the correct order
                cancel.Cancel();
                return x;
            });

            var t3 = q.Enqueue(() =>
            {
                x++;
                return Task.FromResult(x);
            });

            

            Assert.AreEqual(1, await t1);  // completed before cancels
            await Assert.ThrowsExceptionAsync<TaskCanceledException>(() => t3);  // this will have been canceled in the queue
            Assert.AreEqual(2, await t2);  // this will have already started
        }

        [TestMethod]
        public async Task ShouldStopAdding()
        {

            var q = new SerialQueue();

            var x = 0;
            var t1 = q.Enqueue(() =>
            {
                x++;
                return Task.FromResult(x);
            });

            var stop = q.StopAdding();

            // can't add anything new

            Assert.ThrowsException<InvalidOperationException>(() => q.Enqueue(() => true).GetAwaiter().GetResult());

            // doesn't explode...
            // TODO - test that this actually completed t1... 
            await stop;

            // first one still completed
            Assert.AreEqual(1, await t1);
        }

        [TestMethod]
        public async Task ShouldWaitOnStopEmptyQueue()
        {
            var q = new SerialQueue();

            var stop = q.StopAdding();

            // doesn't explode...
            await stop;
        }

        private async Task EnqueueA(bool throwEx)
        {
            if (throwEx)
            {
                throw new ArgumentException();
            }
            await Task.CompletedTask;
        }

        private int EnqueueB(bool throwEx, int val)
        {
            if (throwEx)
            {
                throw new ArgumentException();
            }
            return val;
        }

        private async Task<int> EnqueueC(bool throwEx, int val)
        {
            if (throwEx)
            {
                throw new ArgumentException();
            }
            return await Task.FromResult(val);
        }

        private async void EnqueueD(bool throwEx)
        {
            if (throwEx)
            {
                throw new ArgumentException();
            }
            await Task.CompletedTask;
        }

        [TestMethod]
        public async Task ShouldReturnValues()
        {
            var q = new SerialQueue();

            // task, no results
            var t1 = q.Enqueue(() => EnqueueA(false));
            var t1e = q.Enqueue(() => EnqueueA(true));

            // task, no results - void async
            // failure might derail the rest
            var t4 = q.Enqueue(() =>
            {
                EnqueueD(false);
                return 4;
            });
            var t4e = q.Enqueue(() =>
            {
                EnqueueD(true);
                return 4;
            });

            // func calls with results
            var t2 = q.Enqueue(() => EnqueueB(false, 2));
            var t2e = q.Enqueue(() => EnqueueB(true, 2));

            // task with results
            var t3 = q.Enqueue(() => EnqueueC(false, 3));
            var t3e = q.Enqueue(() => EnqueueC(true, 3));

            await t1;

            var b = await t2;
            Assert.AreEqual(2, b);

            var c = await t3;
            Assert.AreEqual(3, c);

            var d = await t4;
            var de = await t4e;  // doesn't throw because void
            Assert.AreEqual(4, d);
            Assert.AreEqual(4, de);

            await Assert.ThrowsExceptionAsync<ArgumentException>(() => t1e, "1");
            await Assert.ThrowsExceptionAsync<ArgumentException>(() => t2e, "2");
            await Assert.ThrowsExceptionAsync<ArgumentException>(() => t3e, "3");
        }

        [TestMethod]
        public async Task ShouldNotExplode()
        {
            var dist = new Distributor<string, int>();
            var q = new SerialQueue();

            dist.Subscribe<int>("bob", async (a) =>
            {
                await q.Enqueue(async () =>
                {
                    if (a % 5 == 0)
                    {
                        throw new AccessViolationException();
                    }
                    else
                    {
                        await Task.FromResult(0);
                    }
                });

                if (a % 10 == 0)
                {
                    throw new ArgumentException();
                }
            });

            var xcount = 0;
            var tasks = new List<Task>();
            for (var x = 0; x < 1000; x++)
            {
                var z = x;
                var t = Task.Run(async () =>
                {
                    try
                    {
                        await dist.Publish("bob", z);
                    }
                    catch (AccessViolationException)
                    {
                        xcount++;
                    }

                });
                tasks.Add(t);
            }

            await Task.WhenAll(tasks.ToArray());

            Assert.AreEqual(200, xcount);
        }

        [DataTestMethod]
        [DataRow(16, 16)]
        [DataRow(15, 15)]
        [DataRow(7, 7)]
        [DataRow(71, 71)]
        public void ShouldCreateRandomString(int len, int strlen)
        {
            var value = CryptoUtil.GenerateRandomString(len);

            Assert.IsFalse(string.IsNullOrWhiteSpace(value));

            Assert.IsTrue(value.Trim() == value);

            Assert.IsTrue(value.Length == strlen);
        }
    }
}
