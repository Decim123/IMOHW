from django.conf import settings
from django.conf.urls.static import static
from django.contrib import admin
from django.urls import path
from web_hw.views import about, index

urlpatterns = [
    path('', index, name='home'),
    path('about/', about, name='about'),
    path('admin/', admin.site.urls),
]

if settings.DEBUG:
    urlpatterns += static(settings.MEDIA_URL, document_root=settings.MEDIA_ROOT)
