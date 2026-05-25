from django.contrib import messages
from django.contrib.auth import login
from django.contrib.auth.decorators import login_required
from django.contrib.auth.mixins import LoginRequiredMixin, UserPassesTestMixin
from django.shortcuts import get_object_or_404, redirect, render
from django.urls import reverse_lazy
from django.views.generic import DeleteView, DetailView, ListView
from django.views.generic.edit import CreateView, UpdateView

from web_hw.forms import CommentForm, ProductForm, RegisterForm
from web_hw.models import Product, Tag


class ProductListView(ListView):
    model = Product
    template_name = 'web_hw/index.html'
    context_object_name = 'products'
    ordering = ['-created_at']

    def get_queryset(self):
        return super().get_queryset().prefetch_related('tags')

    def get_context_data(self, **kwargs):
        context = super().get_context_data(**kwargs)
        context['title'] = 'Главная страница'
        context['welcome_text'] = 'Добро пожаловать на наш лучший сайт!'
        return context


class ProductsByTagView(ProductListView):
    def get_queryset(self):
        self.tag = get_object_or_404(Tag, slug=self.kwargs['slug'])
        return self.tag.products.prefetch_related('tags').order_by('-created_at')

    def get_context_data(self, **kwargs):
        context = super().get_context_data(**kwargs)
        context['title'] = f'Тег: {self.tag.name}'
        context['welcome_text'] = 'Треки с выбранным тегом'
        context['current_tag'] = self.tag
        return context


class ProductDetailView(DetailView):
    model = Product
    template_name = 'web_hw/detail.html'
    context_object_name = 'product'

    def get_queryset(self):
        return super().get_queryset().prefetch_related('tags')

    def get_context_data(self, **kwargs):
        context = super().get_context_data(**kwargs)
        context['form'] = CommentForm()
        context['comments'] = self.object.comments.order_by('-created_at')
        return context


class ProductCreateView(LoginRequiredMixin, CreateView):
    model = Product
    form_class = ProductForm
    template_name = 'web_hw/form.html'

    def form_valid(self, form):
        form.instance.author = self.request.user
        messages.success(self.request, 'Трек успешно создан.')
        return super().form_valid(form)

    def form_invalid(self, form):
        messages.error(self.request, 'Трек не удалось создать. Проверьте поля формы.')
        return super().form_invalid(form)

    def get_context_data(self, **kwargs):
        context = super().get_context_data(**kwargs)
        context['page_title'] = 'Создание трека'
        return context


class ProductUpdateView(LoginRequiredMixin, UserPassesTestMixin, UpdateView):
    model = Product
    form_class = ProductForm
    template_name = 'web_hw/form.html'

    def test_func(self):
        return self.get_object().author == self.request.user

    def form_valid(self, form):
        messages.success(self.request, 'Трек успешно обновлен.')
        return super().form_valid(form)

    def form_invalid(self, form):
        messages.error(self.request, 'Трек не удалось обновить. Проверьте поля формы.')
        return super().form_invalid(form)

    def get_context_data(self, **kwargs):
        context = super().get_context_data(**kwargs)
        context['page_title'] = 'Редактирование трека'
        return context


class ProductDeleteView(LoginRequiredMixin, UserPassesTestMixin, DeleteView):
    model = Product
    template_name = 'web_hw/product_confirm_delete.html'
    success_url = reverse_lazy('home')

    def test_func(self):
        return self.get_object().author == self.request.user

    def form_valid(self, form):
        messages.success(self.request, 'Трек удален.')
        return super().form_valid(form)


@login_required
def add_comment(request, pk):
    product = get_object_or_404(Product, pk=pk)
    if request.method == 'POST':
        form = CommentForm(request.POST)
        if form.is_valid():
            comment = form.save(commit=False)
            comment.post = product
            comment.author = request.user
            comment.save()
            messages.success(request, 'Комментарий добавлен.')
        else:
            messages.error(request, 'Комментарий не удалось добавить. Проверьте текст.')
    return redirect(product.get_absolute_url())


def register(request):
    if request.method == 'POST':
        form = RegisterForm(request.POST)
        if form.is_valid():
            user = form.save()
            login(request, user)
            messages.success(request, 'Регистрация прошла успешно.')
            return redirect('home')
        messages.error(request, 'Регистрация не удалась. Проверьте данные формы.')
    else:
        form = RegisterForm()
    return render(request, 'registration/register.html', {'form': form})


def about(request):
    return render(request, 'web_hw/about.html')
