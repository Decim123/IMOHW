from dataclasses import dataclass, field


ERR_LENGTH = "length"
ERR_LETTER = "requires_letter"
ERR_DIGIT = "requires_digit"
ERR_SPECIAL = "requires_special"


@dataclass
class PasswordValidationResult:
    is_valid: bool
    errors: list[str] = field(default_factory=list)
    warnings: list[str] = field(default_factory=list)

    def __bool__(self) -> bool:
        return self.is_valid


'''
Требуется проверить минимальную длину пароля (>= 12 символов) и
наличие в пароле хотя бы одной буквы, цифры и спецсимвола.
'''
'''
def validate_password(password: str) -> PasswordValidationResult:
    return PasswordValidationResult(is_valid=True)
'''

def validate_password(password: str) -> PasswordValidationResult:
    errors: list[str] = []

    # Минимальная длина
    if len(password) < 12:
        errors.append(ERR_LENGTH)

    # Хоть одна буква
    if not any(ch.isalpha() for ch in password):
        errors.append(ERR_LETTER)

    # Хоть одна цифра
    if not any(ch.isdigit() for ch in password):
        errors.append(ERR_DIGIT)

    # Хоть один спецсимвол (не буква и не цифра)
    if not any((not ch.isalnum()) for ch in password):
        errors.append(ERR_SPECIAL)

    is_valid = len(errors) == 0
    return PasswordValidationResult(is_valid=is_valid, errors=errors)

# Далее user.py