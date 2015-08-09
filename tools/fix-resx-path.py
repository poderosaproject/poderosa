#!python
# -*- coding: utf-8 -*-
# 
# Fix path in the Resources.resx files.
#

import os
import os.path
import re
import glob
import shutil

def getActualPath(path):
  pattern = re.sub(r'(?<=\\).', r'[\g<0>]', path)
  files = glob.glob(pattern)
  if len(files) != 1:
    print(pattern)
    print(files)
    raise RuntimeError('failed to determine actual file path : %s' % path)
  return files[0]

def fixPath(basePath, path):
  absPath = os.path.normpath(os.path.join(basePath, path))
  actualPath = getActualPath(absPath)
  relPath = os.path.relpath(actualPath, basePath)
  print('orig : %s' % path)
  print('fixed: %s' % relPath)
  return relPath

def fixResxPaths(resxPath):
  tmpResx = resxPath + '.tmp'
  fin = open(resxPath, 'r', encoding='UTF-8-SIG')
  fout = open(tmpResx, 'w', encoding='UTF-8-SIG')
  basePath = os.path.dirname(resxPath)
  phase = 0
  dataName = None
  changed = 0
  for line in fin:
    if phase == 0:
      m = re.match(r'^\s*<data\s+name="(.*?)".*>', line)
      if m is not None:
        dataName = m.group(1)
        phase = 1
    elif phase == 1:
      if re.match(r'^\s*</data>', line) is not None:
        phase = 0
      else:
        m = re.match(r'^(\s*<value>)(.*?)(;.*</value>.*)', line)
        if m is not None:
          dataPath = m.group(2)
          fixedDataPath = fixPath(basePath, dataPath)
          fileBaseName = os.path.basename(fixedDataPath)
          fileName, ext = os.path.splitext(fileBaseName)
          if fileName != dataName:
            raise RuntimeError('data name unmathed : name=%s file=%s (%s)' % (dataName, fileName, fileBaseName))
          if fixedDataPath != dataPath:
            line = m.group(1) + fixedDataPath + m.group(3)
            changed += 1
          dataName = None

    fout.write(line)

  fin.close()
  fout.close()
  if changed > 0:
    shutil.copyfile(tmpResx, resxPath)
  os.unlink(tmpResx)

def convertTree(start, pattern, func):
  regex = re.compile(pattern)
  for path in [
    os.path.join(root, n)
    for root, dirs, files in os.walk(start)
    for n in files if regex.search(n) is not None
  ]:
    func(path)

if __name__ == '__main__':
  start = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
  convertTree(start, r'[Rr]esources\.resx$', fixResxPaths);
