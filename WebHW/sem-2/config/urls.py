from django.conf import settings
from django.conf.urls.static import static
from django.contrib import admin
from django.urls import path
from web_hw.views import about, index, product_create, product_detail, product_update

urlpatterns = [
    path('', index, name='home'),
    path('about/', about, name='about'),
    path('products/create/', product_create, name='product_create'),
    path('products/<int:pk>/', product_detail, name='product_detail'),
    path('products/<int:pk>/edit/', product_update, name='product_update'),
    path('admin/', admin.site.urls),
]

if settings.DEBUG:
    urlpatterns += static(settings.MEDIA_URL, document_root=settings.MEDIA_ROOT)
