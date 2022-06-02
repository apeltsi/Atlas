echo "Caerus Assets Conversion tool"
echo "Converting all png assets to .ktx";
cd ./data/assets
for file in *.png; do
    echo "Converting $file"
    toktx ${file::-4} $file
done
echo "Conversion complete."
while true
do
    read -n 1 -s -r -p "Press any key to close conversion tool
"
    break
done