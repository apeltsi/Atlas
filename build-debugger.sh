cd debugger
pnpm i
pnpm run build
cd dist
mv index.html LogViewer.html
sed -i '$ d' ./LogViewer.html