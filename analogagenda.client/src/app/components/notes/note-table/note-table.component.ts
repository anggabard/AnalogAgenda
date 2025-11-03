import { Component, inject, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { NoteDto, NoteEntryDto, NoteEntryRuleDto, NoteEntryOverrideDto } from '../../../DTOs';
import { NotesService } from '../../../services';
import { TimeHelper } from '../../../helpers/time.helper';

@Component({
    selector: 'app-note-table',
    templateUrl: './note-table.component.html',
    styleUrls: ['./note-table.component.css'],
    standalone: false
})
export class NoteTableComponent implements OnInit {
  private router = inject(Router);
  private notesService = inject(NotesService);

  note: NoteDto = this.getEmptyNote();
  selectedFileName: string | null = null;

  isEditMode = false;
  isNewNote = false;
  isPreviewModalOpen: boolean = false;
  isDeleteModalOpen: boolean = false;

  noteId: string | null = null;
  originalNote: NoteDto | null = null; // Used for discard

  // Film counter for view mode
  filmCount: number = 1;
  
  // UI state for expandable overrides
  expandedEntries: Set<string> = new Set();
  
  // UI state for rule management
  editingRuleForEntry: string | null = null;
  isAddRuleModalOpen = false;
  isEditRuleModalOpen = false;
  selectedStepsForRule: NoteEntryDto[] = [];
  newRule: NoteEntryRuleDto = {
    id: '',
    noteEntryId: '',
    filmInterval: 1,
    timeIncrement: 0.5 // This will display as 0:30
  };
  editingRule: NoteEntryRuleDto | null = null;

  constructor(private route: ActivatedRoute) { }

  ngOnInit(): void {
    this.noteId = this.route.snapshot.paramMap.get('id');

    if (this.noteId) {
      // VIEW / EDIT MODE - Load from backend
      this.loadNoteFromBackend(this.noteId);
    } else {
      // CREATE MODE
      this.isNewNote = true;
      this.isEditMode = true; // allow direct editing when creating
    }
  }

  /** Simulated backend load */
  loadNoteFromBackend(id: string) {
    this.notesService.getById(id).subscribe({
      next: (note: NoteDto) => {
        this.note = note;
        this.originalNote = JSON.parse(JSON.stringify(this.note));
      },
      error: (err: any) => {
        console.error(err);
      }
    });
  }

  /** Switch between view and edit */
  toggleEditMode() {
    if (!this.isEditMode) {
      this.isEditMode = true;
    }
  }

  /** Discard changes and return to original */
  discardChanges() {
    if (this.isNewNote) {
      this.note = this.getEmptyNote();
      this.router.navigate(['/notes']);
    } else if (this.originalNote) {
      this.note = JSON.parse(JSON.stringify(this.originalNote));
    }
    this.isEditMode = false;
  }

  getEmptyNote() {
    return JSON.parse(JSON.stringify({
      id: '',
      name: '',
      sideNote: '',
      imageBase64: '',
      imageUrl: '',
      entries: [{ 
        id: '', 
        noteId: '', 
        time: 0, 
        step: '', 
        details: '', 
        index: 0,
        temperatureMin: 38,
        temperatureMax: undefined,
        rules: [],
        overrides: []
      }]
    }));
  }

  /** Save changes to backend */
  saveNote() {
    if (!this.note.name)
      this.note.name = 'Untitled Note'

    if (this.isNewNote) {
      this.notesService.addNewNote(this.note).subscribe({
        next: (noteId: string) => {
          this.router.navigate(['/notes/' + noteId]);
        },
        error: (err: any) => {
          console.error(err);
        }
      });
    } else {
      this.notesService.update(this.noteId!, this.note).subscribe({
        next: () => {
          this.originalNote = JSON.parse(JSON.stringify(this.note));
          this.isEditMode = false;
        },
        error: (err: any) => {
          console.error(err);
        }
      });
    }
  }

  /** Add a new row */
  addRow() {
    const lastEntry = this.note.entries[this.note.entries.length - 1];
    const newTime = lastEntry ? lastEntry.time : 0;

    this.note.entries.push({
      id: '',
      noteId: '',
      time: newTime,
      step: '',
      details: '',
      index: this.note.entries.length,
      temperatureMin: 38,
      temperatureMax: undefined,
      rules: [],
      overrides: []
    });
  }

  /** Remove an existing row */
  removeRow(index: number) {
    if (this.note.entries.length > 1) {
      this.note.entries.splice(index, 1);
    }
  }

  copyRow(index: number) {
    const originalEntry = this.note.entries[index];
    var copyEntry = JSON.parse(JSON.stringify(originalEntry));
    copyEntry.id = '';
    copyEntry.index = this.note.entries.length;

    this.note.entries.splice(index + 1, 0, copyEntry);
  }

  /** Calculate accumulated start time for an entry based on film count */
  getAccumulatedStartTime(entryIndex: number): number {
    let accumulatedTime = 0;
    for (let i = 0; i < entryIndex; i++) {
      accumulatedTime += this.getEffectiveTime(this.note.entries[i]);
    }
    return accumulatedTime;
  }

  /** Get total time after all steps are completed */
  getTotalTime(): number {
    let totalTime = 0;
    for (let i = 0; i < this.note.entries.length; i++) {
      totalTime += this.getEffectiveTime(this.note.entries[i]);
    }
    return totalTime;
  }

  /** Get applicable override for current film count - returns last override that ended before current film count if outside all ranges */
  getApplicableOverride(entry: NoteEntryDto): NoteEntryOverrideDto | null {
    if (entry.overrides.length === 0) {
      return null;
    }

    // First, try to find an override that matches the current film count
    const matchingOverride = entry.overrides.find(override => 
      this.filmCount >= override.filmCountMin && this.filmCount <= override.filmCountMax
    );
    
    if (matchingOverride) {
      return matchingOverride;
    }
    
    // If no match found, find the last override that ended before the current film count
    // (the override with the highest filmCountMax that is still <= current filmCount)
    const applicableOverrides = entry.overrides.filter(override => 
      (override.filmCountMax ?? 0) <= this.filmCount
    );
    
    if (applicableOverrides.length > 0) {
      // Sort by filmCountMax descending and return the first one
      applicableOverrides.sort((a, b) => (b.filmCountMax ?? 0) - (a.filmCountMax ?? 0));
      return applicableOverrides[0];
    }
    
    // If film count is less than all override mins, return null (use base)
    return null;
  }

  /** Get effective time for an entry based on film count, overrides, and rules */
  getEffectiveTime(entry: NoteEntryDto): number {
    // Check for overrides first
    const applicableOverride = this.getApplicableOverride(entry);
    
    if (applicableOverride && applicableOverride.time !== undefined) {
      return applicableOverride.time;
    }
    
    // Apply rules if no override
    // "Every X films" means: first X films (1-X) get base time only
    // Then every subsequent group of X films gets an additional increment
    // Example (every 3 films): films 1-3: base, 4-6: +x, 7-9: +2x, 10-12: +3x, etc.
    if (entry.rules.length > 0) {
      const rule = entry.rules[0]; // Only one rule per entry
      const incrementCount = Math.floor((this.filmCount - 1) / rule.filmInterval);
      const additionalTime = incrementCount * rule.timeIncrement;
      return entry.time + additionalTime;
    }
    
    return entry.time;
  }

  /** Get effective step name for an entry based on film count and overrides */
  getEffectiveStep(entry: NoteEntryDto): string {
    const applicableOverride = this.getApplicableOverride(entry);
    
    if (applicableOverride && applicableOverride.step) {
      return applicableOverride.step;
    }
    
    return entry.step;
  }

  /** Get effective details for an entry based on film count and overrides */
  getEffectiveDetails(entry: NoteEntryDto): string {
    const applicableOverride = this.getApplicableOverride(entry);
    
    if (applicableOverride && applicableOverride.details) {
      return applicableOverride.details;
    }
    
    return entry.details;
  }

  /** Get effective temperature for an entry based on film count and overrides */
  getEffectiveTemperature(entry: NoteEntryDto): { min: number; max?: number } {
    const applicableOverride = this.getApplicableOverride(entry);
    
    if (applicableOverride) {
      return {
        min: applicableOverride.temperatureMin ?? entry.temperatureMin,
        max: applicableOverride.temperatureMax ?? entry.temperatureMax
      };
    }
    
    return {
      min: entry.temperatureMin,
      max: entry.temperatureMax
    };
  }

  /** Get unique identifier for an entry (use index if id is empty) */
  getEntryIdentifier(entry: NoteEntryDto, index: number): string {
    return entry.id || `temp-${index}`;
  }

  /** Toggle expansion of overrides for an entry */
  toggleOverrideExpansion(entry: NoteEntryDto, index: number) {
    const identifier = this.getEntryIdentifier(entry, index);
    if (this.expandedEntries.has(identifier)) {
      this.expandedEntries.delete(identifier);
    } else {
      this.expandedEntries.add(identifier);
    }
  }

  /** Check if overrides are expanded for an entry */
  isOverrideExpanded(entry: NoteEntryDto, index: number): boolean {
    const identifier = this.getEntryIdentifier(entry, index);
    return this.expandedEntries.has(identifier);
  }

  /** Add a new override to an entry */
  addOverride(entry: NoteEntryDto) {
    // Find the next available film count range
    const nextMin = this.getNextAvailableFilmCountMin(entry);
    
    // Calculate default time: previous override time + 0:15 (15 seconds), or base time + 0:15
    let defaultTime = entry.time + 0.25; // Base time + 0.25 minutes (15 seconds)
    if (entry.overrides.length > 0) {
      // Find the last override and add 0:15 to its time
      const lastOverride = entry.overrides[entry.overrides.length - 1];
      if (lastOverride.time !== undefined && lastOverride.time !== null) {
        defaultTime = lastOverride.time + 0.25; // 0.25 minutes = 15 seconds
      }
    }
    
    const newOverride: NoteEntryOverrideDto = {
      id: '',
      noteEntryId: entry.id,
      filmCountMin: nextMin,
      filmCountMax: nextMin,
      time: defaultTime,
      step: undefined,
      details: undefined,
      temperatureMin: undefined,
      temperatureMax: undefined
    };
    entry.overrides.push(newOverride);
  }

  /** Get the next available minimum film count for a new override - always uses last override's max + 1 */
  private getNextAvailableFilmCountMin(entry: NoteEntryDto): number {
    if (entry.overrides.length === 0) {
      return 1;
    }
    
    // Always use the last override's max + 1
    const sortedOverrides = [...entry.overrides].sort((a, b) => (a.filmCountMin ?? 0) - (b.filmCountMin ?? 0));
    const lastOverride = sortedOverrides[sortedOverrides.length - 1];
    return (lastOverride.filmCountMax ?? 0) + 1;
  }

  /** Check if an override is the first one (first in the sorted order by filmCountMin) */
  isFirstOverride(entry: NoteEntryDto, overrideIndex: number): boolean {
    if (entry.overrides.length === 0 || overrideIndex < 0 || overrideIndex >= entry.overrides.length) {
      return false;
    }
    
    const sortedOverrides = [...entry.overrides].sort((a, b) => (a.filmCountMin ?? 0) - (b.filmCountMin ?? 0));
    const override = entry.overrides[overrideIndex];
    return sortedOverrides[0] === override;
  }

  /** Update subsequent overrides when a previous override's max changes */
  updateSubsequentOverrideMins(entry: NoteEntryDto, changedOverrideIndex: number) {
    const sortedIndices = entry.overrides
      .map((o, i) => ({ override: o, index: i }))
      .sort((a, b) => (a.override.filmCountMin ?? 0) - (b.override.filmCountMin ?? 0));
    
    // Find the position of the changed override in the sorted list
    const changedPosition = sortedIndices.findIndex(item => item.index === changedOverrideIndex);
    
    if (changedPosition < 0 || changedPosition >= sortedIndices.length - 1) {
      return; // No subsequent overrides to update
    }
    
    // Update each subsequent override's min to be the previous override's max + 1
    for (let i = changedPosition + 1; i < sortedIndices.length; i++) {
      const prevOverride = sortedIndices[i - 1].override;
      const currentOverride = sortedIndices[i].override;
      const newMin = (prevOverride.filmCountMax ?? 0) + 1;
      
      if (currentOverride.filmCountMin !== newMin) {
        currentOverride.filmCountMin = newMin;
      }
    }
  }

  /** Check if film count ranges overlap */
  hasOverlappingRanges(entry: NoteEntryDto): boolean {
    const overrides = entry.overrides;
    for (let i = 0; i < overrides.length; i++) {
      for (let j = i + 1; j < overrides.length; j++) {
        const a = overrides[i];
        const b = overrides[j];
        
        // Check if ranges overlap
        if (a.filmCountMin <= b.filmCountMax && b.filmCountMin <= a.filmCountMax) {
          return true;
        }
      }
    }
    return false;
  }

  /** Open Add Rule modal */
  openAddRuleModal() {
    this.isAddRuleModalOpen = true;
    this.selectedStepsForRule = [];
    this.newRule = {
      id: '',
      noteEntryId: '',
      filmInterval: 1,
      timeIncrement: 0.5 // This will display as 0:30
    };
  }

  /** Close Add Rule modal */
  closeAddRuleModal() {
    this.isAddRuleModalOpen = false;
    this.selectedStepsForRule = [];
  }

  /** Get steps that don't have rules and aren't overridden */
  getStepsWithoutRules(): NoteEntryDto[] {
    if (this.isEditRuleModalOpen && this.editingRule) {
      // When editing, show all steps (they can be selected/deselected)
      return this.note.entries;
    }
    return this.note.entries.filter(entry => !this.hasRules(entry) && !this.hasOverrides(entry));
  }

  /** Toggle step selection for rule */
  toggleStepForRule(entry: NoteEntryDto) {
    // Use object reference comparison for reliability
    const index = this.selectedStepsForRule.findIndex(s => s === entry);
    if (index >= 0) {
      this.selectedStepsForRule.splice(index, 1);
    } else {
      this.selectedStepsForRule.push(entry);
    }
  }

  /** Check if step is selected for rule */
  isStepSelectedForRule(entry: NoteEntryDto): boolean {
    // Use object reference comparison for reliability
    return this.selectedStepsForRule.some(s => s === entry);
  }

  /** Remove step from rule selection */
  removeStepFromRule(id: string) {
    this.selectedStepsForRule = this.selectedStepsForRule.filter(s => s.id !== id);
  }

  /** Add rule to selected steps */
  addRuleToSelectedSteps() {
    if (this.selectedStepsForRule.length === 0) return;
    
    this.selectedStepsForRule.forEach(entry => {
      const rule: NoteEntryRuleDto = {
        id: '',
        noteEntryId: entry.id,
        filmInterval: this.newRule.filmInterval,
        timeIncrement: this.newRule.timeIncrement
      };
      entry.rules = [rule]; // Only one rule per entry
    });
    
    this.closeAddRuleModal();
  }

  /** Get all rules grouped by their properties */
  getAllRules(): any[] {
    const ruleMap = new Map<string, { rule: NoteEntryRuleDto, steps: string[] }>();
    
    this.note.entries.forEach(entry => {
      entry.rules.forEach(rule => {
        const key = `${rule.filmInterval}-${rule.timeIncrement}`;
        if (ruleMap.has(key)) {
          ruleMap.get(key)!.steps.push(entry.step);
        } else {
          ruleMap.set(key, { rule, steps: [entry.step] });
        }
      });
    });
    
    return Array.from(ruleMap.values());
  }

  /** Get step names for a rule */
  getRuleSteps(ruleData: any): string[] {
    return ruleData.steps;
  }

  /** Edit rule */
  editRule(rule: NoteEntryRuleDto) {
    this.editingRule = rule;
    this.selectedStepsForRule = this.getStepsForRule(rule);
    this.newRule = {
      id: rule.id,
      noteEntryId: rule.noteEntryId,
      filmInterval: rule.filmInterval,
      timeIncrement: rule.timeIncrement
    };
    this.isEditRuleModalOpen = true;
  }

  /** Get steps for a specific rule */
  getStepsForRule(rule: NoteEntryRuleDto): NoteEntryDto[] {
    return this.note.entries.filter(entry => 
      entry.rules.some(r => 
        r.filmInterval === rule.filmInterval && 
        r.timeIncrement === rule.timeIncrement
      )
    );
  }

  /** Close edit rule modal */
  closeEditRuleModal() {
    this.isEditRuleModalOpen = false;
    this.editingRule = null;
    this.selectedStepsForRule = [];
  }

  /** Save edited rule */
  saveEditedRule() {
    if (!this.editingRule || this.selectedStepsForRule.length === 0) return;
    
    // Get all entries that currently have this rule
    const currentEntriesWithRule = this.note.entries.filter(entry => 
      entry.rules.some(r => 
        r.filmInterval === this.editingRule!.filmInterval && 
        r.timeIncrement === this.editingRule!.timeIncrement
      )
    );
    
    // Remove the rule from entries that are no longer selected
    currentEntriesWithRule.forEach(entry => {
      if (!this.selectedStepsForRule.some(selected => selected.id === entry.id)) {
        entry.rules = entry.rules.filter(r => 
          !(r.filmInterval === this.editingRule!.filmInterval && 
            r.timeIncrement === this.editingRule!.timeIncrement)
        );
      }
    });
    
    // Update rule properties for entries that still have the rule
    this.note.entries.forEach(entry => {
      entry.rules.forEach(rule => {
        if (rule.filmInterval === this.editingRule!.filmInterval && 
            rule.timeIncrement === this.editingRule!.timeIncrement) {
          rule.filmInterval = this.newRule.filmInterval;
          rule.timeIncrement = this.newRule.timeIncrement;
        }
      });
    });
    
    // Add the rule to newly selected entries that don't already have it
    this.selectedStepsForRule.forEach(entry => {
      const hasRule = entry.rules.some(r => 
        r.filmInterval === this.newRule.filmInterval && 
        r.timeIncrement === this.newRule.timeIncrement
      );
      
      if (!hasRule) {
        const newRule: NoteEntryRuleDto = {
          id: '', // Backend will assign this
          noteEntryId: entry.id,
          filmInterval: this.newRule.filmInterval,
          timeIncrement: this.newRule.timeIncrement
        };
        entry.rules.push(newRule);
      }
    });
    
    this.closeEditRuleModal();
  }

  /** Delete rule */
  deleteRule(rule: NoteEntryRuleDto) {
    this.note.entries.forEach(entry => {
      entry.rules = entry.rules.filter(r => r !== rule);
    });
  }

  /** Film counter controls */
  incrementFilmCount() {
    if (this.filmCount < 100) {
      this.filmCount++;
    }
  }

  decrementFilmCount() {
    if (this.filmCount > 1) {
      this.filmCount--;
    }
  }

  /** Remove an override from an entry */
  removeOverride(entry: NoteEntryDto, overrideIndex: number) {
    entry.overrides.splice(overrideIndex, 1);
  }

  /** Add a new rule to an entry */
  addRule(entry: NoteEntryDto) {
    const newRule: NoteEntryRuleDto = {
      id: '',
      noteEntryId: entry.id,
      filmInterval: 1,
      timeIncrement: 0.5 // This will display as 0:30
    };
    entry.rules.push(newRule);
  }

  /** Remove a rule from an entry */
  removeRule(entry: NoteEntryDto) {
    entry.rules = []; // Only one rule per entry
  }

  /** Check if an entry has overrides (rules should be ignored) */
  hasOverrides(entry: NoteEntryDto): boolean {
    return entry.overrides.length > 0;
  }

  /** Check if an entry has rules and no overrides */
  hasRules(entry: NoteEntryDto): boolean {
    return entry.rules.length > 0 && !this.hasOverrides(entry);
  }

  onFileSelected(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (file) {
      this.selectedFileName = file.name;
      const reader = new FileReader();

      reader.readAsDataURL(file);
      reader.onload = () => (this.note.imageBase64 = reader.result as string);
    }
  }

  onDelete() {
    this.notesService.deleteById(this.note.id).subscribe({
      next: () => {
        this.router.navigate(['/notes']);
      },
      error: (err: any) => {
        console.error(err);
      }
    });
  }

  // Time conversion helpers (using TimeHelper utility)
  formatTimeForDisplay(decimalMinutes: number): string {
    return TimeHelper.formatTimeForDisplay(decimalMinutes);
  }


  // Validation helpers for overrides and duration
  isDurationInvalid(entry: NoteEntryDto): boolean {
    return this.isEditMode && (entry.time === undefined || entry.time === null || entry.time <= 0);
  }

  isOverrideTimeInvalid(override: NoteEntryOverrideDto): boolean {
    return override.time === undefined || override.time === null || override.time <= 0;
  }

  isOverrideRangeOrderInvalid(override: NoteEntryOverrideDto): boolean {
    if (override.filmCountMin === undefined || override.filmCountMax === undefined) return false;
    if (override.filmCountMin === null || override.filmCountMax === null) return false;
    return Number(override.filmCountMax) < Number(override.filmCountMin);
  }

  isOverrideRangeOverlapInvalid(entry: NoteEntryDto, index: number): boolean {
    const current = entry.overrides[index];
    const min = Number(current.filmCountMin);
    const max = Number(current.filmCountMax);
    if (isNaN(min) || isNaN(max)) return false;
    return entry.overrides.some((ov, i) => {
      if (i === index) return false;
      const oMin = Number(ov.filmCountMin);
      const oMax = Number(ov.filmCountMax);
      if (isNaN(oMin) || isNaN(oMax)) return false;
      // overlap if ranges intersect
      return min <= oMax && oMin <= max;
    });
  }
}
