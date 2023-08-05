# Custom GUI SFX
Custom GUI SFX is a free and open source Windows tool to wrap an archive file with a size of ~4GB or less in a Windows executable along with custom information, some links, and images, so you will be able to share your files in a more beautiful and personalized way, in addition, you won't need to have any tool installed to unzip the archives because this program makes use of 7-Zip internally (www.7-zip.org)

<br/>
<a href='https://custom-gui-sfx.en.uptodown.com/windows' title='Download Custom GUI SFX' >
  <img src='https://stc.utdstc.com/img/mediakit/download-gio-big-b.png' alt='Download Custom GUI SFX'>
</a>
</br>

## Screenshots
![](screenshots/Screenshot.png)<br/><br/>
![](screenshots/Screenshot%20-%20Extracting.png)
<br/><br/>

## YouTube Video Tutorial
[Custom GUI SFX - Share your files in a more beautiful and personalized way](https://www.youtube.com/watch?v=)

## Why does the archive size have to be ~4GB or less?
I mention the archive because it might be the largest file, but the one that cannot be equal to or larger than 4GB is the final executable, the SFX…

(...)[The executable "image" (the code/data as loaded in memory) of a Win64 file is limited in size to 2GB. This is because the AMD64/EM64T processors use relative addressing for most instructions, and the relative address is kept in a dword. A dword is only capable of holding a relative value of ±2GB](http://www.godevtool.com/GoasmHelp/64bits.htm#diffe)(...)
...a signed dword has a range of 2,147,483,647 and an unsigned dword has a range of 4,294,967,295 [4GB - 1B].

So, when you merge all of your source files (the archive, display images, and the configuration file) with the program executable, make sure that the final size of the executable is less than 4GB. This applies if you are creating your SFX from the command line. If you are creating it from the program, via the Send To context menu, an error will pop up and the SFX will not be created.

## Creating a Custom Graphical Self-Extracting archive
You have two ways of doing this.

The easiest one is to install the program and send the source files to the program as arguments via the Send To context menu, as shown in the [YouTube video tutorial](https://www.youtube.com/watch?v=).

The other one is to use command line utilities such as cat for Unix systems or copy for Windows.

- Example using cat:<br/>
	cat CustomGuiSfx.exe config.txt FirstDisplayImage.gif SecondDisplayImage.png MyArchive.7z > MySFX.exe
	
- Example using copy:<br/>
	copy /b CustomGuiSfx.exe + config.txt + MyArchive.7z MySFX.exe

## Setting up the configuration file
## Notes for developers
