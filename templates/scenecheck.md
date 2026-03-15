;; Template for Scene Check entries
;; Available placeholders: {Type}, {Result}, {Context}, {Details}, {Timestamp}, {Date}, {Time}, {Id}
;; Conditional sections: {?Context}...{/Context} - only shown if Context has a value

| Scene Check | &nbsp; |
| ----------- | ------ |
{?Context}| **Context** | {Context} |
{/Context}| **Result** | {Result} |
{?Details}| *Details* | {Details} |{/Details}
