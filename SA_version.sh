#!/usr/bin/bash
FILELIST=$(grep -rn '<Version>2.2.1</Version>' | cut -f 1 -d ':')
for FILE in $FILELIST; do
	echo ">>> $FILE"
	sed 's/<Version>2.2.1<\/Version>/<Version>2.2.1-SA<\/Version>/g' -i $FILE
done
