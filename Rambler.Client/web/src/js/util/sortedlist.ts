namespace Rambler {

  export function insertSorted<TItem>(items: TItem[], item: TItem, compare: (itemA: TItem, itemB: TItem) => number, unique: boolean) {
    const i = sortedIndex(items, item, compare, unique);
    if (i >= 0) {
      return items.splice(i, 0, item);
    }
    return items;
  }

  export function sortedIndex<TItem>(items: TItem[], item: TItem, compare: (itemA: TItem, itemB: TItem) => number, unique: boolean) {
    let low = 0;
    let high = items.length;
    while (low < high) {
      const mid = (low + high) >>> 1;
      const diff = compare(items[mid], item);
      if (diff < 0) {
        low = mid + 1;
      } else if (unique && diff == 0) {
        return -1; //skip equals
      } else {
        high = mid;
      }
    }
    return low;
  }
}
