import { Component, OnInit, inject, signal } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { mensajeDeError } from '../../core/utils/http-error.util';

/**
 * "Olvide mi contraseña". Atiende dos rutas:
 * - /forgot-password: pide el correo y manda el enlace.
 * - /reset-password?token=...: es a donde lleva el enlace del correo; pide la
 *   contraseña nueva.
 */
@Component({
  selector: 'app-password-reset',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './password-reset.component.html',
  styleUrl: './password-reset.component.css'
})
export class PasswordResetComponent implements OnInit {
  token: string | null = null;
  loading = signal(false);
  error = signal('');
  // Pedido: el correo "salio" (o no existia: la API no lo dice a proposito).
  sent = signal(false);
  // Restablecida: la contraseña ya cambio.
  done = signal(false);
  hide = true;

  private fb = inject(FormBuilder);

  requestForm = this.fb.group({
    email: ['', [Validators.required, Validators.email]]
  });

  resetForm = this.fb.group({
    password: ['', [Validators.required, Validators.minLength(6)]],
    confirm: ['', Validators.required]
  });

  constructor(
    private auth: AuthService,
    private route: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.token = this.route.snapshot.queryParamMap.get('token');
  }

  get mismatch(): boolean {
    const { password, confirm } = this.resetForm.value;
    return !!confirm && password !== confirm;
  }

  submitRequest(): void {
    if (this.requestForm.invalid || this.loading()) return;
    this.loading.set(true);
    this.error.set('');
    this.auth.forgotPassword(this.requestForm.value.email!.trim()).subscribe({
      next: () => { this.loading.set(false); this.sent.set(true); },
      error: (e) => {
        this.loading.set(false);
        this.error.set(mensajeDeError(e, 'No se pudo enviar el correo. Intenta de nuevo.'));
      }
    });
  }

  submitReset(): void {
    if (!this.token || this.resetForm.invalid || this.mismatch || this.loading()) return;
    this.loading.set(true);
    this.error.set('');
    this.auth.resetPassword(this.token, this.resetForm.value.password!).subscribe({
      next: () => { this.loading.set(false); this.done.set(true); },
      error: (e) => {
        this.loading.set(false);
        this.error.set(mensajeDeError(e, 'No se pudo cambiar la contraseña. Pide un enlace nuevo.'));
      }
    });
  }

  goToLogin(): void {
    this.router.navigate(['/login']);
  }
}
