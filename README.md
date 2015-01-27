# directory-content-symlinker
directory-content-symlinker is a Windows command line tool that will search two directories (the target and the destination) for duplicate files. For each duplicate file found, a symbolic link will be created in the destination directory pointing to the file in the target directory. The file in the destination directory will be removed (sent to the Recycle Bin).

### Example
```
C:\symlinker -t "C:\keep-these" -d "C:\replace-these-with-symlinks" -s "*.txt|*.doc"
```

### Arguments
```
-h, --help                 Show this message and exit.
-t, --target=PATH          The PATH to the files symlinks will refer to.
-d, --destination=PATH     The PATH to the files symlinks will be created for.
-s, --searchPattern=VALUE  Pipe (|) delimited list of search patterns used to
                             find files. Each pattern is passed directly to
                             Directory.GetFiles so see docs for that.
```

### Notes
This program uses the mklink command line tool to create symbolic links and will therefore probably have to be run as Administrator.
