import { Component, ElementRef, EventEmitter, HostListener, Input, OnChanges, Output, SimpleChanges, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive } from '@angular/router';

// Menu lateral de movil/tablet (<1024px). No existia antes: hoy la unica
// navegacion en pantallas chicas era la barra inferior. Se abre desde el
// boton hamburguesa de AppHeaderComponent. Contiene lo mismo que el sidebar
// de escritorio (misma idea de grupos), pero como panel deslizante con overlay.
@Component({
  selector: 'app-mobile-drawer',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive],
  templateUrl: './mobile-drawer.component.html',
  styleUrl: './mobile-drawer.component.css'
})
export class MobileDrawerComponent implements OnChanges {
  @Input() open = false;
  @Input() userName = '';
  @Input() userPhotoUrl: string | null = null;

  @Output() closed = new EventEmitter<void>();
  @Output() logoutClick = new EventEmitter<void>();

  @ViewChild('panel') panelRef?: ElementRef<HTMLElement>;

  navLinks = [
    { path: '/dashboard', label: 'Dashboard', icon: 'dashboard' },
    { path: '/objectives', label: 'Objetivos', icon: 'track_changes' },
    { path: '/tasks', label: 'Tareas', icon: 'task_alt' },
    { path: '/statistics', label: 'Estadísticas', icon: 'query_stats' },
  ];

  personalLinks = [
    { path: '/diary', label: 'Diario', icon: 'menu_book', fragment: undefined as string | undefined },
    { path: '/badges', label: 'Logros', icon: 'workspace_premium', fragment: undefined as string | undefined },
    { path: '/community', label: 'Comunidad', icon: 'group', fragment: undefined as string | undefined },
  ];

  ngOnChanges(changes: SimpleChanges): void {
    if (!changes['open']) return;
    document.body.classList.toggle('drawer-open-lock', this.open);
    if (this.open) {
      // Esperamos un tick a que el panel este en el DOM/visible antes de enfocarlo.
      setTimeout(() => this.panelRef?.nativeElement.focus(), 0);
    }
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (this.open) this.close();
  }

  @HostListener('document:keydown', ['$event'])
  onTab(event: KeyboardEvent): void {
    if (event.key !== 'Tab' || !this.open || !this.panelRef) return;
    const focusable = this.panelRef.nativeElement.querySelectorAll<HTMLElement>(
      'a[href], button:not([disabled])'
    );
    if (focusable.length === 0) return;
    const first = focusable[0];
    const last = focusable[focusable.length - 1];
    if (event.shiftKey && document.activeElement === first) {
      event.preventDefault();
      last.focus();
    } else if (!event.shiftKey && document.activeElement === last) {
      event.preventDefault();
      first.focus();
    }
  }

  close(): void {
    this.closed.emit();
    document.body.classList.remove('drawer-open-lock');
    // Devolvemos el foco al boton hamburguesa que abrio el drawer.
    document.getElementById('menu-toggle-btn')?.focus();
  }

  logout(): void {
    this.close();
    this.logoutClick.emit();
  }
}
