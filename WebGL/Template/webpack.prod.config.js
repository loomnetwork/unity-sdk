// This config is used for the production build of the template bundle.

const path = require('path');

module.exports = {
  mode: 'production',
  entry: './asset-transfer.js',
  output: {
    path: path.resolve(__dirname, '../../Assets/WebGLTemplates/Loom'),
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
  }
};