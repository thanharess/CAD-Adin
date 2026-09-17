; AutoLISP code for AutoCAD to delete Dimensions
; Updated: Convert to command-line interface (no DCL), use getkword for choices
; Reordered: 1=selected, 2=by layer, 3=MLEADER, 4=by layer+select, 5=all
; Load this LISP with (load "deletedim.lsp") or APPLOAD

; Main function
(defun c:deletedim (/ choice layname)
  (setq choice nil)
  
  ; Chọn chức năng bằng getkword (có thể click chuột chọn)
  (initget 1 "1 2 3 4 5")
  (setq choice (getkword "\nChọn chức năng [1:Chọn / 2:Theo layer / 3:MLEADER / 4:Theo layer+vùng / 5:Tất cả] <1>: "))
  (if (not choice) (setq choice "1"))
  
  ; Execute based on choice
  (cond
    ((= choice "1")
     (delete-selected-dim)
    )
    ((= choice "2")
     (setq layname (getstring t "\nNhập tên layer cần xóa DIMENSION: "))
     (if (and layname (/= layname ""))
       (delete-dim-by-layer layname)
       (alert "Tên layer không hợp lệ.")
     )
    )
    ((= choice "3")
     (delete-mleader)
    )
    ((= choice "4")
     (setq layname (getstring t "\nNhập tên layer cần xóa DIMENSION: "))
     (if (and layname (/= layname ""))
       (delete-dim-by-layer-select layname)
       (alert "Tên layer không hợp lệ.")
     )
    )
    ((= choice "5")
     (delete-all-dim)
    )
    (T
     (alert "Lựa chọn không hợp lệ. Mặc định xóa DIMENSION được chọn.")
     (delete-selected-dim)
    )
  )
  (princ)
)

; Function 1: Delete selected DIMENSION
(defun delete-selected-dim (/ ss cnt)
  (princ "\nChọn các DIMENSION cần xóa (có thể quét chọn bằng window/crossing hoặc pick từng cái, Enter để kết thúc): ")
  (if (setq ss (ssget '((0 . "DIMENSION"))))
    (progn
      (setq cnt (sslength ss))
      (command "_.ERASE" ss "")
      (alert (strcat "Đã xóa " (itoa cnt) " DIMENSION được chọn."))
    )
    (alert "Không có DIMENSION nào được chọn.")
  )
  (princ)
)

; Function 2: Delete DIMENSION by layer
(defun delete-dim-by-layer (layname / ss filter cnt)
  (setq filter (list '(0 . "DIMENSION") (cons 8 layname)))
  (if (setq ss (ssget "X" filter))
    (progn
      (setq cnt (sslength ss))
      (command "_.ERASE" ss "")
      (alert (strcat "Đã xóa " (itoa cnt) " DIMENSION trên layer " layname "."))
    )
    (alert (strcat "Không có DIMENSION nào trên layer " layname "."))
  )
)

; Function 3: Delete LEADER NOTE (MLEADER)
(defun delete-mleader (/ ss cnt)
  (if (setq ss (ssget "X" '((0 . "MLEADER"))))
    (progn
      (setq cnt (sslength ss))
      (command "_.ERASE" ss "")
      (alert (strcat "Đã xóa " (itoa cnt) " LEADER NOTE (MLEADER)."))
    )
    (alert "Không có LEADER NOTE (MLEADER) nào trong bản vẽ.")
  )
)

; Function 4: Delete DIMENSION by layer and selected area
(defun delete-dim-by-layer-select (layname / filter ss cnt)
  (setq filter (list '(0 . "DIMENSION") (cons 8 layname)))
  (princ (strcat "\nChọn vùng DIMENSION trên layer " layname " (có thể quét chọn bằng window/crossing hoặc pick, Enter để kết thúc): "))
  (if (setq ss (ssget filter))
    (progn
      (setq cnt (sslength ss))
      (command "_.ERASE" ss "")
      (alert (strcat "Đã xóa " (itoa cnt) " DIMENSION trên layer " layname " trong vùng chọn."))
    )
    (alert (strcat "Không có DIMENSION nào được chọn trên layer " layname "."))
  )
  (princ)
)

; Function 5: Delete all DIMENSION
(defun delete-all-dim (/ ss cnt)
  (if (setq ss (ssget "X" '((0 . "DIMENSION"))))
    (progn
      (setq cnt (sslength ss))
      (command "_.ERASE" ss "")
      (alert (strcat "Đã xóa " (itoa cnt) " DIMENSION."))
    )
    (alert "Không có DIMENSION nào trong bản vẽ.")
  )
)

(princ "\nGõ deletedim để chạy lệnh. (Cập nhật: Không dùng DCL, dùng command-line với getkword)")
(princ)