import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { DiaryEntry, CreateDiaryEntryDto, UpdateDiaryEntryDto } from '../models/diary-entry.model';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class DiaryEntryService {
  private url = `${environment.apiUrl}/diaryentries`;

  constructor(private http: HttpClient) {}

  getAll(userId: number): Observable<DiaryEntry[]> {
    return this.http.get<DiaryEntry[]>(`${this.url}?userId=${userId}`);
  }

  getById(id: number): Observable<DiaryEntry> {
    return this.http.get<DiaryEntry>(`${this.url}/${id}`);
  }

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
