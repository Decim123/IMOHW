# Generated manually for task 10.

from django.conf import settings
import django.db.models.deletion
from django.db import migrations, models


def assign_existing_products_to_superuser(apps, schema_editor):
    app_label, model_name = settings.AUTH_USER_MODEL.split('.')
    User = apps.get_model(app_label, model_name)
    Product = apps.get_model('web_hw', 'Product')

    user = User.objects.filter(is_superuser=True).order_by('id').first()
    if user is None:
        user = User.objects.order_by('id').first()
    if user is None:
        user = User.objects.create(
            username='admin',
            password='!',
            is_staff=True,
            is_superuser=True,
        )

    Product.objects.filter(author__isnull=True).update(author=user)


class Migration(migrations.Migration):

    dependencies = [
        migrations.swappable_dependency(settings.AUTH_USER_MODEL),
        ('web_hw', '0004_comment'),
    ]

    operations = [
        migrations.RenameField(
            model_name='product',
            old_name='author',
            new_name='artist',
        ),
        migrations.AddField(
            model_name='product',
            name='author',
            field=models.ForeignKey(
                null=True,
                on_delete=django.db.models.deletion.CASCADE,
                related_name='products',
                to=settings.AUTH_USER_MODEL,
            ),
        ),
        migrations.RunPython(assign_existing_products_to_superuser, migrations.RunPython.noop),
        migrations.AlterField(
            model_name='product',
            name='author',
            field=models.ForeignKey(
                on_delete=django.db.models.deletion.CASCADE,
                related_name='products',
                to=settings.AUTH_USER_MODEL,
            ),
        ),
    ]
