from django import forms


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
