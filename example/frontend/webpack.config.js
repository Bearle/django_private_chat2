const path = require("path");
const MiniCssExtractPlugin = require("mini-css-extract-plugin");
const webpack = require("webpack");
const OptimizeCssAssetsPlugin = require("optimize-css-assets-webpack-plugin");
const { ESBuildPlugin, ESBuildMinifyPlugin } = require('esbuild-loader')

module.exports = function(_env, argv) {
  const isProduction = argv.mode === "production";
  const isDevelopment = !isProduction;

  return {
    devtool: isDevelopment && "cheap-module-source-map",
    entry: "./src/index.js",
    output: {
      path: path.resolve(__dirname, "../static"),
      filename: "js/[name].js",
      publicPath: "/"
    },
    module: {
      rules: [
          {
         test: /\.js$/,
         loader: 'esbuild-loader',
         exclude: /node_modules/,
         options: {
           loader: 'jsx', // Remove this if you're not using JSX
           target: 'es2015', // Syntax to compile to (see options below for possible values),
         }
       },
        {
          test: /\.css$/,
          use: [
            isProduction ? MiniCssExtractPlugin.loader : "style-loader",
            "css-loader"
          ]
        },
        {
          test: /\.(png|jpg|gif)$/i,
          use: {
            loader: "url-loader",
            options: {
              limit: 8192,
              name: "static/media/[name].[ext]"
            }
          }
        },
        {
          test: /\.svg$/,
          use: ["@svgr/webpack"]
        },
        {
          test: /\.(eot|otf|ttf|woff|woff2)$/,
          loader: require.resolve("file-loader"),
          options: {
            name: "static/media/[name].[hash:8].[ext]"
          }
        }
      ]
    },
    resolve: {
      extensions: [".js", ".jsx"]
    },
    plugins: [
      isProduction &&
        new MiniCssExtractPlugin({
          filename: "css/[name].css",
          chunkFilename: "acss/[name].chunk.css"
        }),
      new ESBuildPlugin(),
      new webpack.DefinePlugin({
        "process.env.NODE_ENV": JSON.stringify(
          isProduction ? "production" : "development"
        )
      })
    ].filter(Boolean),
    optimization: {
      minimize: isProduction,
      minimizer: [
        new ESBuildMinifyPlugin({target: 'es2015', minify:isProduction}),
        new OptimizeCssAssetsPlugin()
      ],
      // splitChunks: {
      //   chunks: "all",
      //   minSize: 0,
      //   maxInitialRequests: 10,
      //   maxAsyncRequests: 10,
      //   cacheGroups: {
      //     vendors: {
      //       test: /[\\/]node_modules[\\/]/,
      //       name(module, chunks, cacheGroupKey) {
      //         const packageName = module.context.match(
      //           /[\\/]node_modules[\\/](.*?)([\\/]|$)/
      //         )[1];
      //         return `${cacheGroupKey}.${packageName.replace("@", "")}`;
      //       }
      //     },
      //     common: {
      //       minChunks: 2,
      //       priority: -10
      //     }
      //   }
      // },
      // runtimeChunk: "single"
    },
    devServer: {
      compress: true,
      historyApiFallback: true,
      open: false,
      overlay: true
    }
  };
};
