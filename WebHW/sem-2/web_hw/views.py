from django.shortcuts import render


def index(request):
    context = {
        'title': 'Главная страница',
        'welcome_text': 'Добро пожаловать на наш лучший сайт!',
    }
    return render(request, 'web_hw/index.html', context)


def about(request):
    return render(request, 'web_hw/about.html')
