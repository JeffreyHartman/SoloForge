;; Template for Fate Check entries
;; Available placeholders: {Type}, {Result}, {Context}, {Details}, {Timestamp}, {Date}, {Time}, {Id}
;; Conditional sections: {?Context}...{/Context} - only shown if Context has a value

| Fate Check | &nbsp; |
| ---------- | ------ |
| **Question** | {Context} |
| **Result** | {Result} |
{?Details}| *Details* | {Details} |{/Details}
