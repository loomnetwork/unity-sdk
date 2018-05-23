// This config is used for the development build of the template bundle.

/*
  The development flow is assumed to work like this:
  1. Do a WebGL build in Unity and specify webgl-build as the build output dir.
  2. yarn build:dev to do a Webpack build using this config, the bundle will be generated in the
     webgl-build dir.
  3. Hard-reload of the WebGL build to reload the bundle.
  4. Make some changes to the code, run yarn build:dev again, hard reload.
*/

const path = require('path');

module.exports = {
  mode: 'development',
  entry: './asset-transfer.js',
  output: {
    // Generate bundle in the webgl-build dir
    path: path.resolve(__dirname, '../../webgl-build'),
    filename: 'bundle.js',
    libraryTarget: 'var',
    library: 'LoomTemplateBundle'
  },
  module: {
    rules: [
      {
        test: /\.(js)$/,
        exclude: /(node_modules)/,
        use: 'babel-loader'
      }
    ]
  },
};
