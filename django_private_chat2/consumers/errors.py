import enum
from typing import Tuple


class ErrorTypes(enum.IntEnum):
    MessageParsingError = 1
    TextMessageInvalid = 2
    InvalidMessageReadId = 3
    InvalidUserPk = 4
    InvalidRandomId = 5
    FileMessageInvalid = 6
    FileDoesNotExist = 7


ErrorDescription = Tuple[ErrorTypes, str]
