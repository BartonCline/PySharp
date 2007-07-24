# Makefile for the PythonRuntime .NET assembly and tests. Thanks to
# Camilo Uribe <kmilo@softhome.net> for contributing Mono support.
#
# When you are using Mono don't forget to add this line to /etc/mono/config:
#  <dllmap dll="python25" target="/usr/lib/libpython2.5.so.1.0" os="!windows"/>
# Thanks to angel ignacio colmenares laguado

RELEASE=pythonnet-2.0-alpha2-py2.5-clr2.0-src
RUNNER=
ILDASM=ildasm
ILASM=ilasm
CSC=csc.exe
#PYTHONVER=PYTHON24
PYTHONVER=PYTHON25
#PYTHONVER=PYTHON26
# unicode width
UCS=UCS2
#UCS=UCS4

all: python.exe clr.pyd Python.Test.dll

cleanall: clean all

python.exe: Python.Runtime.dll
	cd src; cd console; \
	$(CSC) /define:$(PYTHONVER),$(UCS) /nologo /target:exe /out:../../python.exe \
        /reference:../../Python.Runtime.dll /recurse:*.cs
	cd ..; cd ..;


Python.Runtime.dll:
	cd src; cd runtime; \
	$(CSC) /define:$(PYTHONVER),$(UCS) /nologo /unsafe /target:library \
        /out:../../Python.Runtime.dll /recurse:*.cs
	cd ..; cd ..;


clr.pyd: Python.Runtime.dll
	$(ILASM) /nologo /dll /quiet /output=clr.pyd \
	./src/runtime/clrmodule.il;


clr.so: Python.Runtime.dll src/monoclr/clrmod.c src/monoclr/pynetclr.h \
    src/monoclr/pynetinit.c
	python setup.py build_ext -i


Python.Test.dll: Python.Runtime.dll
	cd src; cd testing; \
	$(CSC) /define:$(PYTHONVER),$(UCS) /nologo /target:library \
	/out:../../Python.Test.dll \
	/reference:../../Python.Runtime.dll,System.Windows.Forms.dll \
	/recurse:*.cs
	cd ..; cd ..;


clean:
	find . \( -name \*.o -o -name \*.so -o -name \*.py[co] -o -name \
		\*.dll -o -name \*.exe -o -name \*.pdb -o -name \*.mdb \
		-o -name \*.pyd -o -name \*~ \) -exec rm -f {} \;
	rm -f Python*.il Python*.il2 Python*.res
	rm -rf build/
	cd src/console; rm -rf bin; rm -rf obj; cd ../..;
	cd src/runtime; rm -rf bin; rm -rf obj; cd ../..;
	cd src/testing; rm -rf bin; rm -rf obj; cd ../..;
	cd src/embed_tests; rm -rf bin; rm -rf obj; rm -f TestResult.xml; cd ../..;
	cd src/monoclr; make clean; cd ../..


test: all
	rm -f ./src/tests/*.pyc
	$(RUNNER) ./python.exe ./src/tests/runtests.py


dist: clean all
	rm -rf ./$(RELEASE)
	mkdir ./$(RELEASE)
	mkdir -p ./release
	cp ./makefile ./$(RELEASE)/
	cp ./*.sln ./$(RELEASE)/
	cp ./*.txt ./$(RELEASE)/
	svn export ./demo ./$(RELEASE)/demo/
	svn export ./doc ./$(RELEASE)/doc/
	svn export ./src ./$(RELEASE)/src/
	cp ./python.exe ./$(RELEASE)/
	cp ./*.dll ./$(RELEASE)/
	cp ./*.pyd ./$(RELEASE)/
	tar czf $(RELEASE).tgz ./$(RELEASE)/
	mv $(RELEASE).tgz ./release/
	rm -rf ./$(RELEASE)/


dis:
	$(ILDASM) Python.Runtime.dll /out=Python.Runtime.il


asm:
	$(ILASM) /dll /quiet  \
	/resource=Python.Runtime.res /output=Python.Runtime.dll \
	Python.Runtime.il


ucs:
	# system python
	python -c "from distutils.sysconfig import get_config_var; \
	           print 'UCS%i' % get_config_var('Py_UNICODE_SIZE')"


monoclr:
	cd src/monoclr; make; cd ../../

