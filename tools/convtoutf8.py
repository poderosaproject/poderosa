#!python
# -*- coding: utf-8 -*-
# 
# Convert text encoding of the source files to UTF-8 with BOM.
# Assumes CP932 as the old text encoding.
#

import os
import os.path
import re

def isUTF8SIG(path):
  f = open(path, 'rb')
  v = f.read(3)
  f.close()
  if v == b'\xef\xbb\xbf':
    return True
  return False

def cp932ToUTF8(path):
  f = open(path, 'rb')
  content = f.read()
  f.close()
  try:
    content = content.decode(encoding='cp932', errors='strict')
    content = u'\ufeff' + content
    content = content.encode(encoding='UTF-8', errors='strict')
  except UnicodeError:
    return False
  
  f = open(path, 'wb')
  f.write(content)
  f.close()
  return True

def convertTree(start, pattern):
  regex = re.compile(pattern)
  for path in [
    os.path.join(root, n)
    for root, dirs, files in os.walk(start)
    for n in files if regex.search(n) is not None
  ]:
    if not isUTF8SIG(path):
      converted = cp932ToUTF8(path)
      if converted:
        print('CONVERTED:', path)
      else:
        print('SKIPPED:', path)


if __name__ == '__main__':
  start = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
  convertTree(start, r'\.(cs|sln|csproj|resx)$');
