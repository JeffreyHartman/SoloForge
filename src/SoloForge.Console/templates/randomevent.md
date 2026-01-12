;; Template for Random Event entries
;; Available placeholders: {Type}, {Result}, {Context}, {Details}, {Timestamp}, {Date}, {Time}, {Id}
;; Conditional sections: {?Details}...{/Details} - only shown if Details has a value

| Random Event | &nbsp; |
| ------------ | ------ |
| **Event** | {Result} |
{?Details}| *Details* | {Details} |{/Details}
