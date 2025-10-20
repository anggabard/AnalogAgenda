import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class LocalStorageService {
  constructor() { }

  saveState(key: string, state: any): void {
    try {
      localStorage.setItem(key, JSON.stringify(state));
    } catch (error) {
      console.error(`Error saving state for key ${key}:`, error);
    }
  }

  getState(key: string): any {
    try {
      const state = localStorage.getItem(key);
      return state ? JSON.parse(state) : null;
    } catch (error) {
      console.error(`Error loading state for key ${key}:`, error);
      return null;
    }
  }

  clearState(key: string): void {
    try {
      localStorage.removeItem(key);
    } catch (error) {
      console.error(`Error clearing state for key ${key}:`, error);
    }
  }

  hasState(key: string): boolean {
    try {
      return localStorage.getItem(key) !== null;
    } catch (error) {
      console.error(`Error checking state for key ${key}:`, error);
      return false;
    }
  }
}
