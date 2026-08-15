import { Component, OnDestroy, OnInit, signal } from '@angular/core';
import { CommonModule, Location } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-auth',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './auth.component.html',
  styleUrl: './auth.component.css'
})
export class AuthComponent implements OnInit, OnDestroy {
  isRegister = false;
  hideLogin = true;
  hideRegister = true;

  loginForm: FormGroup;
  registerForm: FormGroup;
  loginError = signal('');
  registerError = signal('');
  loading = signal(false);

  // La base es Azure SQL serverless y se pausa sola cuando no se usa. La primera
  // peticion del dia tiene que esperar a que despierte y puede tardar cerca de un
  // minuto. Pasados unos segundos avisamos, para que no parezca que se colgo.
  slowLoading = signal(false);
  private slowTimer?: ReturnType<typeof setTimeout>;

  private startLoading() {
    this.loading.set(true);
    this.slowLoading.set(false);
    this.slowTimer = setTimeout(() => this.slowLoading.set(true), 4000);
  }

  private stopLoading() {
    this.loading.set(false);
    this.slowLoading.set(false);
    clearTimeout(this.slowTimer);
  }

  // Un error de red o del servidor no significa que las credenciales esten mal:
  // distinguirlos evita mandar al usuario a dudar de su contraseña cuando en
  // realidad no se pudo llegar al backend.
  private mensajeDeError(status: number, porDefecto: string): string {
    if (status === 400 || status === 401) return porDefecto;
    if (status === 0 || status === 504) return 'No se pudo conectar con el servidor. Puede estar iniciandose: espera unos segundos y vuelve a intentar.';
    if (status === 429) return 'Demasiados intentos. Espera un momento antes de volver a probar.';
    return 'Hubo un problema en el servidor. Intenta de nuevo en unos segundos.';
  }

  constructor(
    private fb: FormBuilder,
    private auth: AuthService,
    private router: Router,
    private route: ActivatedRoute,
    private location: Location
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
    if (this.route.snapshot.queryParamMap.get('sessionExpired')) {
      this.loginError.set('Tu sesión expiró. Volvé a iniciar sesión.');
    }
  }

  switchTo(mode: 'login' | 'register') {
    this.isRegister = mode === 'register';
    this.loginError.set('');
    this.registerError.set('');
    this.location.replaceState(mode === 'register' ? '/register' : '/login');
  }

  submitLogin() {
    if (this.loginForm.invalid) return;
    this.startLoading();
    this.loginError.set('');
    this.auth.login(this.loginForm.value).subscribe({
      next: () => { this.stopLoading(); this.router.navigate(['/dashboard']); },
      error: (e) => {
        this.loginError.set(this.mensajeDeError(e?.status, 'Usuario o contraseña incorrectos'));
        this.stopLoading();
      }
    });
  }

  submitRegister() {
    if (this.registerForm.invalid) return;
    this.startLoading();
    this.registerError.set('');
    this.auth.register(this.registerForm.value).subscribe({
      next: () => { this.stopLoading(); this.switchTo('login'); },
      error: (e) => {
        this.registerError.set(e?.error?.detail ?? this.mensajeDeError(e?.status, 'Error al registrarse. Intenta de nuevo.'));
        this.stopLoading();
      }
    });
  }

  ngOnDestroy() {
    clearTimeout(this.slowTimer);
  }
}
