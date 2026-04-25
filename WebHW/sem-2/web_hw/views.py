from django.shortcuts import render

from web_hw.models import Product


def index(request):
    context = {
        'title': 'Главная страница',
        'welcome_text': 'Добро пожаловать на наш лучший сайт!',
        'products': Product.objects.order_by('-created_at'),
    }
    return render(request, 'web_hw/index.html', context)


def about(request):
    return render(request, 'web_hw/about.html')
