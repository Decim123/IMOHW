from django.contrib import admin
from django.urls import path
from web_hw.views import index

urlpatterns = [
    path('', index, name='home'),
    path('admin/', admin.site.urls),
]
