import random

class Student:
    def __init__(self, name, group, performance):
        self.name = name
        self.group = group
        self.performance = performance 

    def average_grade(self):
        if not self.performance:
            return 0
        return sum(self.performance.values()) / len(self.performance)

    def __str__(self):
        return f"Студент: {self.name}, группа {self.group}, средний балл: {self.average_grade():.2f}"

class Faculty:
    def __init__(self, name, specialization, teachers_count):
        self.name = name
        self.specialization = specialization
        self.teachers_count = teachers_count
        self.students = []

    def add_student(self, student):
        self.students.append(student)

    def average_faculty_grade(self):
        if not self.students:
            return 0
        return sum(st.average_grade() for st in self.students) / len(self.students)

    def __str__(self):
        return (f"Факультет: {self.name} ({self.specialization}) | "
                f"Преподавателей: {self.teachers_count} | Студентов: {len(self.students)}")

class University:
    def __init__(self, name, address, budget):
        self.name = name
        self.address = address
        self.budget = budget
        self.faculties = []

    def add_faculty(self, faculty):
        self.faculties.append(faculty)

    def total_students(self):
        return sum(len(f.students) for f in self.faculties)

    def __str__(self):
        return (f"Университет: {self.name}\n"
                f"Адрес: {self.address}\n"
                f"Бюджет: {self.budget:,} Рублей\n"
                f"Факультетов: {len(self.faculties)} | Студентов: {self.total_students()}\n")

def x():
    return random.randint(0, 5)

if __name__ == "__main__":

    s1 = Student("Александр Первый", "ИТ-101", {"Математика": x(), "Программирование": x(), "Физика": x()})
    s2 = Student("Александр Второй", "ИТ-101", {"Математика": x(), "Программирование": x(), "Физика": x()})
    s3 = Student("Александр Третий", "ЭК-201", {"Экономика": x(), "Статистика": x(), "Право": x()})

    it_faculty = Faculty("Факультет ИТ", "Информационные технологии", 3)
    econ_faculty = Faculty("Факультет Экономики", "Экономика и финансы", 5)

    it_faculty.add_student(s1)
    it_faculty.add_student(s2)
    econ_faculty.add_student(s3)

    uni = University("МПГУ", "г. Москва ул...", 12000000)
    uni.add_faculty(it_faculty)
    uni.add_faculty(econ_faculty)

    print(uni)
    for f in uni.faculties:
        print(f)
        for st in f.students:
            print("-", st)
        print(f"--Средний балл по факультету: {f.average_faculty_grade():.2f}\n")