# Generated manually for task 12.

from django.conf import settings
import django.db.models.deletion
from django.db import migrations, models


def assign_existing_comments_to_user(apps, schema_editor):
    app_label, model_name = settings.AUTH_USER_MODEL.split('.')
    User = apps.get_model(app_label, model_name)
    Comment = apps.get_model('web_hw', 'Comment')

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

    Comment.objects.filter(author__isnull=True).update(author=user)


class Migration(migrations.Migration):

    dependencies = [
        migrations.swappable_dependency(settings.AUTH_USER_MODEL),
        ('web_hw', '0006_tag_product_tags'),
    ]

    operations = [
        migrations.RenameField(
            model_name='comment',
            old_name='product',
            new_name='post',
        ),
        migrations.AddField(
            model_name='comment',
            name='author',
            field=models.ForeignKey(
                null=True,
                on_delete=django.db.models.deletion.CASCADE,
                related_name='comments',
                to=settings.AUTH_USER_MODEL,
            ),
        ),
        migrations.RunPython(assign_existing_comments_to_user, migrations.RunPython.noop),
        migrations.RemoveField(
            model_name='comment',
            name='email',
        ),
        migrations.RemoveField(
            model_name='comment',
            name='subject',
        ),
        migrations.AlterField(
            model_name='comment',
            name='author',
            field=models.ForeignKey(
                on_delete=django.db.models.deletion.CASCADE,
                related_name='comments',
                to=settings.AUTH_USER_MODEL,
            ),
        ),
    ]
