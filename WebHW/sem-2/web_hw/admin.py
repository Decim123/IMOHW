from django.contrib import admin

from web_hw.models import Product


@admin.register(Product)
class ProductAdmin(admin.ModelAdmin):
    list_display = ('title', 'author', 'price', 'created_at')
