import zipfile
import os
import sys

outfile = "BuildArtefacts\\NAudio-Demo-Apps.zip"
if len(sys.argv) > 1:
    outfile = sys.argv[1]
print "creating " + outfile

folders = ['AudioFileInspector','NAudioDemo','NAudioWpfDemo']
files = {}

def exclude(filename):
    return filename.endswith('.pdb') or ('.vshost.' in filename)

for folder in folders:
    fullpath = folder + "\\bin\\debug\\"
    for filename in os.listdir(fullpath):
        if not exclude(filename):
            files[filename] = fullpath + filename

zip = zipfile.ZipFile(outfile, "w")

for filename, fullpath in files.iteritems():
    if os.path.isdir(fullpath):
        #print fullpath + " is a folder"
        for subfile in os.listdir(fullpath):
            zip.write(fullpath + "\\" + subfile, filename + "\\" + subfile, zipfile.ZIP_DEFLATED)
    else:
        zip.write(fullpath, filename, zipfile.ZIP_DEFLATED)

zip.close()
