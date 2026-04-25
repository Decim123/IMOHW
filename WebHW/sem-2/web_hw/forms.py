from django import forms

from web_hw.models import Product


class FeedbackForm(forms.Form):
    subject = forms.CharField(
        label='Имя',
        max_length=200,
        widget=forms.TextInput(attrs={'class': 'form-control'})
    )
    email = forms.EmailField(
        label='Почта',
        widget=forms.EmailInput(attrs={'class': 'form-control'})
    )
    text = forms.CharField(
        label='Комментарий',
        widget=forms.Textarea(attrs={'class': 'form-control', 'rows': 5})
    )


class ProductForm(forms.ModelForm):
    class Meta:
        model = Product
        fields = ['title', 'author', 'price', 'image', 'lyrics']
        widgets = {
            'title': forms.TextInput(attrs={'class': 'form-control'}),
            'author': forms.TextInput(attrs={'class': 'form-control'}),
            'price': forms.NumberInput(attrs={'class': 'form-control', 'step': '0.01'}),
            'image': forms.ClearableFileInput(attrs={'class': 'form-control'}),
            'lyrics': forms.Textarea(attrs={'class': 'form-control', 'rows': 10}),
        }
