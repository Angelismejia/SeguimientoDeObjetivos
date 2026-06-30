
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Category, CreateCategoryDto, UpdateCategoryDto } from '../models/category.model';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class CategoryService {
  private url = `${environment.apiUrl}/categories`;

  constructor(private http: HttpClient) {}

  getAll(userId: number): Observable<Category[]> {
    return this.http.get<Category[]>(`${this.url}?userId=${userId}`);
  }

  create(data: CreateCategoryDto): Observable<Category> {
    return this.http.post<Category>(this.url, data);
  }

  update(id: number, data: UpdateCategoryDto): Observable<Category> {
    return this.http.put<Category>(`${this.url}/${id}`, data);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.url}/${id}`);
  }
}
