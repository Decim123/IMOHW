from django.shortcuts import get_object_or_404, render

from web_hw.models import Product


def index(request):
    context = {
        'title': 'Главная страница',
        'welcome_text': 'Добро пожаловать на наш лучший сайт!',
        'products': Product.objects.order_by('-created_at'),
    }
    return render(request, 'web_hw/index.html', context)


def product_detail(request, pk):
    product = get_object_or_404(Product, pk=pk)
    return render(request, 'web_hw/detail.html', {'product': product})


def about(request):
    return render(request, 'web_hw/about.html')
