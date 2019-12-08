;-----------------------------
; emports
;-----------------------------
global _main
EXPORT _main
;-----------------------------
; imports
;-----------------------------
extern _printf
extern _ExitProcess@4
;-----------------------------
; initialized data
;-----------------------------
section .data USE32
;-----------------------------
; uninitialized data
;-----------------------------
section .bss USE32
	str:	resb	128
	num2:	resd	1
	num3:	resd	1
	num1:	resd	1
	aray:	resd	1
;-----------------------------
; code
;-----------------------------
section .code USE32
_main:
	mov	DWORD[num1],	3
	mov	DWORD[num2],	-1
exit:
	mov	eax,	0x0
	call	_ExitProcess@4
