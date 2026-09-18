;;;=============================================
;;; Lệnh: SCALEDIMVALUE
;;; Tác dụng: Scale giá trị "Dim scale linear" (DIMLFAC)
;;; Phiên bản sửa: Mode 3 đặt tất cả về 1 (không nhân/chia)
;;;=============================================

(vl-load-com)

(defun c:SCALEDIMVALUE ( / ss mode factor i en obj old new)
  (prompt "\nLệnh SCALEDIMVALUE - Scale Dim Linear factor.")

  ;; Chọn các DIM (cho phép chọn trên locked layers nếu cần)
  (prompt "\nChọn các DIMENSION cần scale (Enter để kết thúc): ")
  (setq ss (ssget '((0 . "DIMENSION"))))
  (if (not ss)
    (progn (prompt "\nKhông có DIM nào được chọn.") (exit))
  )

  ;; Chọn mode bằng getkword (hiển thị menu dưới prompt, có thể click chuột chọn 1/2/3)
  (initget 1 "1 2 3")
  (setq mode (getkword "\nChọn chế độ [1:Tăng / 2:Giảm / 3:1:1] <1>: "))
  (if (not mode) (setq mode "1"))

  ;; Xử lý nhập hệ số chỉ cho mode 1 và 2; mode 3 không cần
  (cond
    ((= mode "1") 
     (setq factor (getreal "\nNhập hệ số tăng (VD: 2 = nhân 2:1): "))
     (if (not factor) (setq factor 1.0))
    )
    ((= mode "2") 
     (setq factor (getreal "\nNhập hệ số giảm (VD: 5 = chia 1:5): "))
     (if factor (setq factor (/ 1.0 factor)) (setq factor 1.0))
    )
    ((= mode "3") (setq factor 1.0))  ; Không dùng factor, chỉ đánh dấu
  )

  (if (or (not factor) (/= mode "3"))
    (if (not factor)
      (progn (prompt "\nKhông có hệ số hợp lệ.") (exit))
    )
  )

  ;; Lặp qua từng dim để scale LinearScaleFactor
  (setq i (sslength ss))
  (while (> i 0)
    (setq i (1- i))
    (setq en (ssname ss i))
    (setq obj (vlax-ename->vla-object en))
    (cond
      ((= mode "3")
       (vla-put-LinearScaleFactor obj 1.0)
      )
      (T
       (setq old (vla-get-LinearScaleFactor obj))
       (setq new (* old factor))
       (vla-put-LinearScaleFactor obj new)
      )
    )
  )

  (cond
    ((= mode "3")
     (prompt (strcat "\nĐã đặt " (itoa (sslength ss)) " DIM về giá trị 1.0"))
    )
    (T
     (prompt (strcat "\nĐã scale " (itoa (sslength ss))
                     " DIM, hệ số nhân: " (rtos factor 2 4)))
    )
  )
  (princ)
)

(princ "\nGõ SCALEDIMVALUE để chạy lệnh. (Sử dụng chuột click vào menu dưới prompt để chọn nhanh)")
(princ)