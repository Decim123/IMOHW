from django.conf import settings
from django.conf.urls.static import static
from django.contrib import admin
from django.urls import include, path
from web_hw.views import (
    ProductCreateView,
    ProductDeleteView,
    ProductDetailView,
    ProductListView,
    ProductUpdateView,
    ProductsByTagView,
    about,
    add_comment,
    register,
)

urlpatterns = [
    path('', ProductListView.as_view(), name='home'),
    path('about/', about, name='about'),
    path('accounts/', include('django.contrib.auth.urls')),
    path('accounts/register/', register, name='register'),
    path('create/', ProductCreateView.as_view(), name='product_create_short'),
    path('tags/<slug:slug>/', ProductsByTagView.as_view(), name='products_by_tag'),
    path('products/create/', ProductCreateView.as_view(), name='product_create'),
    path('products/<int:pk>/', ProductDetailView.as_view(), name='product_detail'),
    path('products/<int:pk>/comments/add/', add_comment, name='add_comment'),
    path('products/<int:pk>/edit/', ProductUpdateView.as_view(), name='product_update'),
    path('products/<int:pk>/delete/', ProductDeleteView.as_view(), name='product_delete'),
    path('admin/', admin.site.urls),
]

if settings.DEBUG:
    urlpatterns += static(settings.MEDIA_URL, document_root=settings.MEDIA_ROOT)
