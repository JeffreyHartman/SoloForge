;; Template for Meaning Roll entries
;; Available placeholders: {Type}, {Result}, {Context}, {Details}, {Timestamp}, {Date}, {Time}, {Id}
;; Conditional sections: {?Context}...{/Context} - only shown if Context has a value

| Meaning Roll | &nbsp; |
| ------------ | ------ |
{?Context}| **For** | {Context} |
{/Context}| **Result** | {Result} |
{?Details}| *Details* | {Details} |{/Details}
