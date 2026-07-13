import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../core/services/auth.service';
import { UserService } from '../../core/services/user.service';
import { User } from '../../core/models/user.model';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './profile.component.html',
  styleUrl: './profile.component.css'
})
export class ProfileComponent implements OnInit {
  loading = signal(true);
  loadError = signal(false);
  uploading = signal(false);
  uploadError = signal('');

  user = signal<User | null>(null);

  private apiOrigin = environment.apiUrl.replace('/api', '');

  constructor(
    private auth: AuthService,
    private userService: UserService
  ) {}

  ngOnInit(): void {
    this.loadUser();
  }

  private loadUser(): void {
    this.loading.set(true);
    this.loadError.set(false);
    this.userService.getById(this.auth.getUserId()).subscribe({
      next: user => {
        this.user.set(user);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.loadError.set(true);
      }
    });
  }

  photoUrl(): string | null {
    const url = this.user()?.profilePhotoUrl;
    return url ? this.apiOrigin + url : null;
  }

  onPhotoSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;

    this.uploading.set(true);
    this.uploadError.set('');
    this.userService.uploadPhoto(this.auth.getUserId(), file).subscribe({
      next: updated => {
        this.user.set(updated);
        this.uploading.set(false);
        input.value = '';
      },
      error: () => {
        this.uploading.set(false);
        this.uploadError.set('No se pudo subir la foto. Probá con una imagen JPG, PNG o WEBP.');
        input.value = '';
      }
    });
  }
}
