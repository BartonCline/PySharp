#!/usr/bin/env python
from setuptools import setup
from setuptools import find_packages
from setuptools import Extension

import commands
def pkgconfig(*packages, **kw):
    """From http://aspn.activestate.com/ASPN/Cookbook/Python/Recipe/502261
    """
    flag_map = {'-I': 'include_dirs', '-L': 'library_dirs', '-l': 'libraries'}
    output = commands.getoutput("pkg-config --libs --cflags %s" % ' '.join(packages)).split()
    for token in output:
        if flag_map.has_key(token[:2]):
            kw.setdefault(flag_map.get(token[:2]), []).append(token[2:])
        else: # throw others to extra_link_args
            kw.setdefault('extra_link_args', []).append(token)
    for k, v in kw.iteritems(): # remove duplicated
        kw[k] = list(set(v))
    return kw

clr = Extension('clr',
    ['src/monoclr/clr.c'],
    **pkgconfig('mono')
    )

setup(name="clr",
    ext_modules = [clr],
    )
