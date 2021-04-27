var gulp = require('gulp');
var gutil = require('gulp-util');
var sass = require('gulp-sass');
var ts = require('gulp-typescript');
var plumber = require('gulp-plumber');
var minify = require('gulp-minify');
var inject = require('gulp-inject');
var del = require('del');

var isProd = true;
var jsExt = '.min.js';
var jsConfig = 'config/env.ts';

var sourcePaths = {
  index: 'src/index.html',
  intro: 'src/intro.html',
  htaccess: 'src/.htaccess',
  manifest: 'src/manifest.json',
  libs: [
    // 'node_modules/angular/angular' + jsExt,
    // 'node_modules/angular-route/angular-route' + jsExt,
    'node_modules/ngstorage/ngStorage' + jsExt,
    'src/js/libs/*.js',
  ],
  styles: ['src/styles/**/*.scss'],
  ts: [jsConfig, 'src/**/*.ts'],
  views: ['src/views/**/*.html'],
  assets: ['src/assets/**/*.*']
};

var sassConfig = {
  errLogToConsole: true,
  outputStyle: 'expanded',
  includePaths: 'node_modules/purecss/build'
};

function configure(prod, config) {
  isProd = prod;
  jsExt = isProd ? '.min.js' : '.js';
  jsConfig = 'config/env.' + config + '.ts';

  sourcePaths = {
    index: 'src/index.html',
    intro: 'src/intro.html',
    htaccess: 'src/.htaccess',
    manifest: 'src/manifest.json',
    libs: [
      // 'node_modules/angular/angular' + jsExt,
      // 'node_modules/angular-route/angular-route' + jsExt,
      'node_modules/ngstorage/ngStorage' + jsExt,
      'src/js/libs/*.js',
    ],
    styles: ['src/styles/**/*.scss'],
    ts: [jsConfig, 'src/**/*.ts'],
    views: ['src/views/**/*.html'],
    assets: ['src/assets/**/*.*']
  };

  sassConfig = {
    errLogToConsole: true,
    outputStyle: isProd ? 'compressed' : 'expanded',
    includePaths: 'node_modules/purecss/build'
  };
}

console.log(sourcePaths);

const dist = 'build';
var distPaths = {
  htaccess: dist,
  styles: dist + '/css',
  js: dist + '/js',
  libs: dist + '/js/libs',
  views: dist + '/views',
  assets: dist + '/assets',
};

gulp.task('clean', gulp.series(function () {
  return del([dist]);
}));

function copyFiles(src, dest) {
  return gulp
    .src(src)
    .pipe(gulp.dest(dest));
}

gulp.task('copy-libs', gulp.series(function () {
  return copyFiles(sourcePaths.libs, distPaths.libs);
}));

gulp.task('copy-htaccess', gulp.series(function () {
  return copyFiles(sourcePaths.htaccess, distPaths.htaccess);
}));

gulp.task('copy-assets', gulp.series(function () {
  return copyFiles(sourcePaths.assets, distPaths.assets);
}));

gulp.task('copy-views', gulp.series(function () {
  return copyFiles(sourcePaths.views, distPaths.views);
}));

gulp.task('copy-manifest', gulp.series(function () {
  return copyFiles(sourcePaths.manifest, dist);
}));

gulp.task('copy-intro', gulp.series(function () {
  return copyFiles(sourcePaths.intro, dist);
}));

function streamTypescript() {
  return gulp
    .src(sourcePaths.ts)
    .pipe(ts({ outFile: 'chat.js' }))
    .js;
}

function streamSass() {
  return gulp
    .src(sourcePaths.styles)
    .pipe(plumber())
    .pipe(sass(sassConfig));
}

gulp.task('build-sass', gulp.series(function () {
  return streamSass()
    .pipe(gulp.dest(distPaths.styles));
}));

gulp.task('build-typescript', gulp.series(function () {
  return streamTypescript()
    .pipe(isProd
      ? minify({ ext: { min: '.min.js' }, noSource: true, })
      : gutil.noop())
    .pipe(gulp.dest(distPaths.js));
}));


gulp.task('build-index', gulp.series(function () {
  // add the source paths to the index
  // this handles the *.min.* names, but otherwise pretty pointless.
  var sources = gulp.src([
    distPaths.styles + '/**/*.css',
    distPaths.libs + '/*js',
    distPaths.js + '/*js',
  ], { read: false });

  return gulp.src(sourcePaths.index)
    .pipe(inject(sources, {
      addRootSlash: false,
      removeTags: true,
      ignorePath: dist,
    }))
    .pipe(gulp.dest(dist));
}));

gulp.task('generate-service-worker', gulp.series(['copy-manifest', 'copy-libs', 'copy-assets', 'copy-views', 'build-sass', 'build-typescript', 'build-index'], function (callback) {
  var swPrecache = require('sw-precache');

  swPrecache.write(dist + '/sw.js', {
    staticFileGlobs: [dist + '/**/*.{json,js,html,css,png,jpg,gif,svg,eot,ttf,woff}'],
    stripPrefix: dist
  }, callback);
}));

gulp.task('build', gulp.series(['clean', 'copy-intro', 'copy-manifest', 'copy-libs', 'copy-assets', 'copy-htaccess', 'copy-views', 'build-sass', 'build-typescript', 'build-index', 'generate-service-worker']));

gulp.task('watch', gulp.series(['build'], function () {
  gulp.watch(sourcePaths.assets, { cwd: './' }, ['copy-assets', 'generate-service-worker']);
  gulp.watch(sourcePaths.views, { cwd: './' }, ['copy-views', 'generate-service-worker']);
  gulp.watch(sourcePaths.styles, { cwd: './' }, ['build-sass', 'generate-service-worker']);
  gulp.watch(sourcePaths.ts, { cwd: './' }, ['build-typescript', 'generate-service-worker']);
}));

gulp.task('uat', gulp.series(function (cb) {
  configure(false, 'uat');
  return gulp.series('build', cb);
}));

// gulp.task('prod', gulp.series(function (cb) {
//   configure(true, 'prod');
//   return runSequence('build', cb);
// }));

