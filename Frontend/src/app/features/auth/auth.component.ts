import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-auth',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './auth.component.html',
  styleUrl: './auth.component.css'
})
export class AuthComponent implements OnInit {
  isRegister = false;
  hideLogin = true;
  hideRegister = true;

  loginForm: FormGroup;
  registerForm: FormGroup;
  loginError = '';
  registerError = '';
  loading = false;

  constructor(
    private fb: FormBuilder,
    private auth: AuthService,
    private router: Router
  ) {
    this.loginForm = this.fb.group({
      username: ['', Validators.required],
      password: ['', Validators.required]
    });
    this.registerForm = this.fb.group({
      name:     ['', Validators.required],
      username: ['', Validators.required],
      email:    ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]]
    });
  }

  ngOnInit() {
    this.isRegister = this.router.url.includes('register');
  }

  switchTo(mode: 'login' | 'register') {
    this.isRegister = mode === 'register';
    this.loginError = '';
    this.registerError = '';
    this.router.navigate([mode === 'register' ? '/register' : '/login'], { replaceUrl: true });
  }

  submitLogin() {
    if (this.loginForm.invalid) return;
    this.loading = true;
    this.loginError = '';
    this.auth.login(this.loginForm.value).subscribe({
      next: () => this.router.navigate(['/dashboard']),
      error: () => { this.loginError = 'Usuario o contraseña incorrectos'; this.loading = false; }
    });
  }

  submitRegister() {
    if (this.registerForm.invalid) return;
    this.loading = true;
    this.registerError = '';
    this.auth.register(this.registerForm.value).subscribe({
      next: () => this.switchTo('login'),
      error: (e) => {
        this.registerError = e?.error?.detail ?? 'Error al registrarse. Intenta de nuevo.';
        this.loading = false;
      }
    });
  }
}
