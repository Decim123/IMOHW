from django.shortcuts import get_object_or_404, redirect, render

from web_hw.forms import FeedbackForm
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


def about(request):
    return render(request, 'web_hw/about.html')
