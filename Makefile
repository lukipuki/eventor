all: People.exe

%.exe: %.cs
        dmcs -r:System.Xml.Linq $<
