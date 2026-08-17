import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { App } from './app';

describe('App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideRouter([]), provideHttpClient()]
    }).compileComponents();
  });

  it('el componente raiz se crea', () => {
    const fixture = TestBed.createComponent(App);
    expect(fixture.componentInstance).toBeTruthy();
  });

  // El test original venia del andamiaje de `ng new` y buscaba un <h1> que
  // dijera "Hello, Frontend". Esa plantilla se reemplazo en junio por el shell
  // real de la app, asi que el test fallaba describiendo algo que ya no existe.
  it('monta el shell de navegacion, no la plantilla inicial', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    const html = (fixture.nativeElement as HTMLElement).innerHTML;

    expect(html).not.toContain('Hello, Frontend');
    expect(fixture.nativeElement.querySelector('router-outlet')).toBeTruthy();
  });
});
