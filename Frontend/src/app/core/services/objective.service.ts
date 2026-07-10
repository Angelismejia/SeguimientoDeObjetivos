import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Objective, CreateObjectiveDto, UpdateObjectiveDto } from '../models/objective.model';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class ObjectiveService {
  private url = `${environment.apiUrl}/objectives`;

  constructor(private http: HttpClient) {}

  getAll(userId: number): Observable<Objective[]> {
    return this.http.get<Objective[]>(`${this.url}?userId=${userId}`);
  }

  getById(id: number): Observable<Objective> {
    return this.http.get<Objective>(`${this.url}/${id}`);
  }

  create(data: CreateObjectiveDto, userId: number): Observable<Objective> {
    return this.http.post<Objective>(`${this.url}?userId=${userId}`, data);
  }

  update(id: number, data: UpdateObjectiveDto): Observable<Objective> {
    return this.http.put<Objective>(`${this.url}/${id}`, data);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.url}/${id}`);
  }
}
