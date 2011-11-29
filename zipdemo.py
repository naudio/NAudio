import zipfile
import os

folders = ['AudioFileInspector','NAudioDemo','NAudioWpfDemo']
files = {}

def exclude(filename):
    return filename.endswith('.pdb') or ('nunit' in filename)

for folder in folders:
    fullpath = folder + "\\bin\\debug\\"
    for filename in os.listdir(fullpath):
        if not exclude(filename):
            files[filename] = fullpath + filename

zip = zipfile.ZipFile("BuildArtefacts\\test.zip", "w")

for filename, fullpath in files.iteritems():
    if os.path.isdir(fullpath):
        #print fullpath + " is a folder"
        for subfile in os.listdir(fullpath):
            zip.write(fullpath + "\\" + subfile, filename + "\\" + subfile)
    else:
        zip.write(fullpath, filename)

zip.close()
