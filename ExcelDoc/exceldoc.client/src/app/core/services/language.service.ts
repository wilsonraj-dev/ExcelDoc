import { HttpClient } from '@angular/common/http';
import { Injectable, Injector } from '@angular/core';
import { Observable, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { TranslateService } from './translate.service';

const LANG_KEY = 'user_lang';
const VALID_LANGS = ['pt', 'en', 'es'];

@Injectable({ providedIn: 'root' })
export class LanguageService {
  private readonly languageApiUrl = '/api/usuarios/idioma';

  constructor(private translate: TranslateService, private http: HttpClient) { }

  inicializar(idioma: string | null | undefined) {
    const lang = idioma && VALID_LANGS.includes(idioma) ? idioma : localStorage.getItem(LANG_KEY) ?? 'pt';
    this.translate.use(lang);
    localStorage.setItem(LANG_KEY, lang);
  }

  trocarIdioma(lang: string): Observable<void> {
    if (!VALID_LANGS.includes(lang)) {
      console.error('Idioma inválido', lang);
      return of();
    }

    this.translate.use(lang);
    localStorage.setItem(LANG_KEY, lang);

    try {
      //const http = this.injector.get(HttpClient);
      return this.http.put<void>(this.languageApiUrl, { idioma: lang }).pipe(
        catchError(err => {
          console.error('Falha ao atualizar idioma no backend', err);
          return of();
        })
      );
    } catch (err) {
      console.error('Não foi possível obter HttpClient para atualizar idioma', err);
      return of();
    }
  }

  get idiomaAtual(): string {
    return localStorage.getItem(LANG_KEY) ?? 'pt';
  }
}
