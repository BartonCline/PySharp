# Makefile for the PythonRuntime .NET assembly and tests. Thanks to
# Camilo Uribe <kmilo@softhome.net> for contributing Mono support.

RELEASE=pythonnet-2.0-alpha2-py2.5-clr2.0-src
RUNNER=
ILDASM=ildasm
ILASM=ilasm
CSC=csc.exe

all: python.exe clr.pyd Python.Test.dll


python.exe: Python.Runtime.dll
	cd src; cd console; \
	$(CSC) /nologo /target:exe /out:../../python.exe \
        /reference:../../Python.Runtime.dll /recurse:*.cs
	cd ..; cd ..;


Python.Runtime.dll:
	cd src; cd runtime; \
	$(CSC) /nologo /unsafe /target:library /out:../../Python.Runtime.dll \
        /recurse:*.cs
	cd ..; cd ..;


clr.pyd: Python.Runtime.dll
	$(ILASM) /nologo /dll /quiet /output=clr.pyd \
	./src/runtime/clrmodule.il;


Python.Test.dll: Python.Runtime.dll
	cd src; cd testing; \
	$(CSC) /nologo /target:library /out:../../Python.Test.dll \
	/reference:../../Python.Runtime.dll \
	/recurse:*.cs
	cd ..; cd ..;


clean:
	rm -f python.exe Python*.dll Python*.il Python*.il2 Python*.res
	rm -f CLR.dll
	rm -f ./*~
	cd src; cd console; rm -f *~; cd ..; cd ..;
	cd src; cd runtime; rm -f *~; cd ..; cd ..;
	cd src; cd testing; rm -f *~; cd ..; cd ..;
	cd src; cd tests; rm -f *~; rm -f *.pyc; cd ..; cd ..;
	cd doc; rm -f *~; cd ..;
	cd demo; rm -f *~; cd ..;


test:
	rm -f ./src/tests/*.pyc
	$(RUNNER) ./python.exe ./src/tests/runtests.py


dist: clean
	mkdir ./$(RELEASE)
	cp -R ./makefile ./$(RELEASE)/
	cp -R ./demo ./$(RELEASE)/
	cp -R ./doc ./$(RELEASE)/
	cp -R ./src ./$(RELEASE)/
	make
	cp ./python.exe ./$(RELEASE)/
	cp ./*.dll ./$(RELEASE)/
	tar czf $(RELEASE).tgz ./$(RELEASE)/
	mv $(RELEASE).tgz ./release/
	rm -rf ./$(RELEASE)/


dis:
	$(ILDASM) Python.Runtime.dll /out=Python.Runtime.il


asm:
	$(ILASM) /dll /quiet  \
	/resource=Python.Runtime.res /output=Python.Runtime.dll \
	Python.Runtime.il



