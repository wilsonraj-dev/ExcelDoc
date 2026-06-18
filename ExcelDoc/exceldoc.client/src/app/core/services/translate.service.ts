import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class TranslateService {
  private translations: Record<string, any> = {};
  private current = 'pt';

  constructor(private http: HttpClient) {
    this.load(this.current);
  }

  use(lang: string) {
    this.current = lang;
    if (!this.translations[lang]) {
      this.load(lang);
    }
  }

  instant(key: string): string {
    const segs = key.split('.');
    let obj = this.translations[this.current] ?? {};
    for (const s of segs) {
      obj = obj?.[s];
      if (obj == null) return key;
    }
    return String(obj);
  }

  private load(lang: string) {
    this.http.get(`/i18n/${lang}.json`).subscribe({
      next: (data) => (this.translations[lang] = data),
      error: () => (this.translations[lang] = {})
    });
  }
}
