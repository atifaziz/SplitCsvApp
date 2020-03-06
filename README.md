# CSV Splitter Utility

`splitcsv` is a simple command-line utility whose sole purpose is to help
split a large CSV file into multiple smaller files where each file will
contain a user-specified number of rows.

The headers are repeated in the split files such that each CSV can be treated
independently.

Each split file carries the original name of the CSV file followed by `-N`
where _N_ is the split part number starting with 1. The paths to the generated
splits are printed on separate lines to standard output.

The format of split files always uses double-quotes (`"`) around fields and
escapes any double-quotes embedded in a field's value by doubling (`""`).

If a CSV is too small to split, it will perform no action.


## Usage

```
splitcsv <OPTIONS> <FILE>...

options:

  -?, --help, -h             prints out the options
      --verbose, -v          enable additional output
  -d, --debug                debug break
  -e, --encoding=VALUE       input/output file encoding
  -l, --lines=VALUE          lines per split (10,000)
      --od, --output-dir=VALUE
                             output directory (default is same as source)
      --ap, --absolute-paths emit absolute paths to split files
```
