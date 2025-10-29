class Character:
    def __init__(self, character_name, hp):
        self.character_name = character_name
        self.hp = hp

    def attack_description(self):
        raise NotImplementedError("Этот метод должен быть реализован в подклассах!")

class Warrior(Character):
    def __init__(self, character_name, hp, strength):
        super().__init__(character_name, hp)
        self.strength = strength

    def attack_description(self):
        return f"{self.character_name} атакует противника силой удара {self.strength} единиц."

class Wizard(Character):
    def __init__(self, character_name, hp, magic_power):
        super().__init__(character_name, hp)
        self.magic_power = magic_power

    def attack_description(self):
        return f"{self.character_name} накладывает заклинание с мощностью магии {self.magic_power} единиц."

if __name__ == "__main__":
    warrior = Warrior("Александр Великий", 120, 35)
    wizard = Wizard("Александр Не такой Влеикий", 90, 80)

    print(warrior.attack_description())
    print(wizard.attack_description())