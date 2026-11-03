import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { DiaryEntry, CreateDiaryEntryDto, UpdateDiaryEntryDto } from '../models/diary-entry.model';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class DiaryEntryService {
  private url = `${environment.apiUrl}/diaryentries`;

  constructor(private http: HttpClient) {}

  /**
   * Las `limit` entradas anteriores a `before` (sin cursor: las mas recientes).
   * El cursor es fecha + id porque el diario se ordena por la fecha que elige el
   * usuario, y puede haber varias entradas el mismo dia.
   */
  getPage(userId: number, limit: number, before?: DiaryEntry): Observable<DiaryEntry[]> {
    const cursor = before
      ? `&beforeDate=${encodeURIComponent(before.entryDate)}&beforeId=${before.id}`
      : '';
    return this.http.get<DiaryEntry[]>(`${this.url}?userId=${userId}&limit=${limit}${cursor}`);
  }

  /** Cuantas entradas tiene el usuario autenticado, sin bajar el texto. */
  count(): Observable<number> {
    return this.http.get<number>(`${this.url}/count`);
  }

  getById(id: number): Observable<DiaryEntry> {
    return this.http.get<DiaryEntry>(`${this.url}/${id}`);
  }

  /** El autor sale del token en el backend, no se manda por la URL. */
  create(data: CreateDiaryEntryDto): Observable<DiaryEntry> {
    return this.http.post<DiaryEntry>(this.url, data);
  }

  update(id: number, data: UpdateDiaryEntryDto): Observable<DiaryEntry> {
    return this.http.put<DiaryEntry>(`${this.url}/${id}`, data);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.url}/${id}`);
  }
}
