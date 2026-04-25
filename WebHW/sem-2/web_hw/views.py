from django.shortcuts import get_object_or_404, redirect, render

from web_hw.forms import FeedbackForm, ProductForm
from web_hw.models import Comment, Product


def index(request):
    context = {
        'title': 'Главная страница',
        'welcome_text': 'Добро пожаловать на наш лучший сайт!',
        'products': Product.objects.order_by('-created_at'),
    }
    return render(request, 'web_hw/index.html', context)


def product_detail(request, pk):
    product = get_object_or_404(Product, pk=pk)
    if request.method == 'POST':
        form = FeedbackForm(request.POST)
        if form.is_valid():
            print(form.cleaned_data)
            Comment.objects.create(
                product=product,
                subject=form.cleaned_data['subject'],
                email=form.cleaned_data['email'],
                text=form.cleaned_data['text'],
            )
            return redirect(product.get_absolute_url())
    else:
        form = FeedbackForm()
    context = {
        'product': product,
        'form': form,
        'comments': product.comments.order_by('-created_at'),
    }
    return render(request, 'web_hw/detail.html', context)


def product_create(request):
    if request.method == 'POST':
        form = ProductForm(request.POST, request.FILES)
        if form.is_valid():
            product = form.save()
            return redirect(product.get_absolute_url())
    else:
        form = ProductForm()
    return render(request, 'web_hw/form.html', {'form': form, 'page_title': 'Создание трека'})


def product_update(request, pk):
    product = get_object_or_404(Product, pk=pk)
    if request.method == 'POST':
        form = ProductForm(request.POST, request.FILES, instance=product)
        if form.is_valid():
            product = form.save()
            return redirect(product.get_absolute_url())
    else:
        form = ProductForm(instance=product)
    context = {
        'form': form,
        'page_title': 'Редактирование трека',
        'product': product,
    }
    return render(request, 'web_hw/form.html', context)


def about(request):
    return render(request, 'web_hw/about.html')
