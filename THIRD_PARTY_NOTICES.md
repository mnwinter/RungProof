# Third-party notices

## Three.js

- Project: Three.js
- Upstream tag: `r179`
- License: MIT
- Source: `https://github.com/mrdoob/three.js/tree/r179`
- Local license: `vendor/three/LICENSE`

Vendored runtime files:

```text
vendor/three/three.module.min.js
SHA-256 06552C54E4071FBC7305117AAFE6765D92C5D2A2A83507D4F05B9BF4F3D4D463

vendor/three/three.core.min.js
SHA-256 79F2B4F58D3E99A9948A4D3B7F6D5C2DAF705BDEFE9FB82EBEC715623966551C

vendor/three/addons/controls/OrbitControls.js
SHA-256 7181C3EC9E1283E1AF930C30EC56FA664FEFD491C765D8C6877C20304D2F6BA2
```

`OrbitControls.js` has one local import-path change so it loads the vendored
Three.js module without an import map. No behavioral code was changed.

## Python

- Version: `3.14.5`
- License: Python Software Foundation License Version 2
- Source and license: `https://docs.python.org/3.14/license.html`
- Local license: `vendor/licenses/PYTHON-3.14-LICENSE.txt`

PyInstaller embeds the Python runtime in the Windows executable.

## PySide6 and Qt

- Version: `6.11.1`
- License option used by this build: LGPL 3.0 only
- PySide6 source: `https://code.qt.io/cgit/pyside/pyside-setup.git/`
- Qt source and licensing: `https://www.qt.io/licensing/open-source-lgpl-obligations`
- License inventory:
  `https://doc.qt.io/qtforpython-6/licenses.html`

RungProof uses PySide6, Shiboken6, Qt Widgets, and Qt 3D. The Windows package
uses PyInstaller's one-folder mode so the Qt shared libraries remain separate
files in the extracted application folder. No QtWebEngine module is used or
packaged.

Before external commercial distribution, the product owner must complete a Qt
LGPL compliance review, include the applicable full license texts and source
offer, preserve user replacement/relinking rights, and confirm whether a
commercial Qt license is required. This internal engineering package is not a
completed commercial-distribution compliance package.

## PyInstaller

- Version: `6.21.0`
- License: GPL 2.0 or later with the PyInstaller bootloader exception
- Source: `https://github.com/pyinstaller/pyinstaller/tree/v6.21.0`
- Local license: `vendor/licenses/PYINSTALLER-LICENSE.txt`

The bootloader exception permits distributing applications produced with
PyInstaller without applying the GPL to the application itself.

## python-snap7

- Version: `3.1.0`
- License: MIT
- Source: `https://github.com/gijzelaerr/python-snap7/tree/3.1.0`
- Local license: `vendor/licenses/PYTHON-SNAP7-LICENSE.txt`

Version 3.1.0 is the pure-Python S7 implementation and does not bundle the
older native Snap7 library.

## jsonschema

- Version: `4.25.1`
- License: MIT
- Source: `https://github.com/python-jsonschema/jsonschema/tree/v4.25.1`
- Local license: `vendor/licenses/JSONSCHEMA-LICENSE.txt`

The local save adapter uses jsonschema to enforce the versioned Draft 2020-12
scene contract before writing files.

## Siemens PLC/PC Interface snapshot

- Upstream: `https://github.com/mnwinter/Siemens-PLC-PC-Interface`
- Commit: `754fcfb88192f2a932bd7df70feea0d08088ab97`
- Local provenance: `vendor/siemens-plc-pc-interface/PROVENANCE.md`

This is project-owned source reused at a fixed revision, plus one documented
read-only diagnostic method. It replaces the prior mutable sibling-checkout
build dependency.
